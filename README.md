# Let's Ship - PriceFalcon

The aim of this repository is to share the experience of developing and shipping a working .NET 5 application including deployments, dev-ops, migrations, etc.

This will be a learning experience for me and hopefully the write up will help people get up to speed with .NET 5 if they've never used it before, including deploying to Linux machines.

The application we're building is an MVC web application backed by a PostgresSQL data store. The application allows users to submit arbitrary URLs of product pages from other sites to monitor for price changes and receive email alerts.

The motivation here is to build and deloy a non-trivial site running in .NET 5. Like most people working in software I have a complete inability to ship a finished product. This tutorial aims to serve a motivation to reach that goal and also act as a blueprint for early stage products in the .NET space to make slightly better architecture and deployment decisions.

We'll be deploying our application using a single node kubernetes cluster. I used to believe this was overengineering and kubernetes was a fad. But using k8s in a single node cluster gives up a couple of advantages that make it worth the up-front investment even for an MVP.

- Zero downtime deployments using rolling updates. I've done enough weekend and evening deployments in my life. Our field (software) has a seriously unhealthy attitude to working hours. I should be able to push out a deployment in working hours and go home to do what I care about, not have to wait until outside working hours to make updates. If your engineers can't leave the office at 5pm you're doing something wrong.
- Reproducible environments using docker. This isn't unique to k8s and there are other ways to deploy docker images but by containerising your application even with a single instance you ensure that you can test the same environment that runs in production. Fewer nasty surprises related to environment differences.
- Load balancing and room to grow. Your site idea is going to fail, 99% of the time. But if not this set-up should give you a solid foundation for the first 2 years.

## Step 1 (you say we need to talk)

First, find out what version of .NET we're running, from the command line run:

```
dotnet --version
```

If you get:

```
'dotnet' is not recognized as an internal or external command,
operable program or batch file.
```

Then you haven't got it installed yet, go grab and download it. https://dotnet.microsoft.com/download/dotnet/5.0 I'm using version `5.0.102` which was the latest as of March 9th 2021.

Let's type `dotnet new` to see what we can create.

```
-- Snip --
ASP.NET Core Empty                                web                      [C#], F#          Web/Empty
ASP.NET Core Web App (Model-View-Controller)      mvc                      [C#], F#          Web/MVC
ASP.NET Core Web App                              webapp                   [C#]              Web/MVC/Razor Pages
ASP.NET Core with Angular                         angular                  [C#]              Web/MVC/SPA
ASP.NET Core with React.js                        react                    [C#]              Web/MVC/SPA
ASP.NET Core with React.js and Redux              reactredux               [C#]              Web/MVC/SPA
Razor Class Library                               razorclasslib            [C#]              Web/Razor/Library
-- Snip --
```

Since I'm still old-school and not up to speed with Razor yet let's create a new MVC app. Inside the `src` folder I created (`mkdir src` on Windows) I'm going to run a command to get details about what options I can specify:

```
dotnet new mvc --help
```

Most of the options in the result correspond to Azure Active Directory or other authentication options. Since I can't be bothered with authentication (yet) I'll ignore these (No authentication is the default when using `dotnet new mvc` in my version). I do however want to specify the project name (`-n`) and the output location (`-o`) so I'm going to run:

```
dotnet new mvc -n PriceFalcon.Web -o PriceFalcon.Web
```

And the output shows we were successful:

```
C:\git\csharp\LetsShip\src>dotnet new mvc -n PriceFalcon.Web -o PriceFalcon.Web
The template "ASP.NET Core Web App (Model-View-Controller)" was created successfully.
This template contains technologies from parties other than Microsoft, see https://aka.ms/aspnetcore/5.0-third-party-notices for details.

Processing post-creation actions...
Running 'dotnet restore' on PriceFalcon.Web\PriceFalcon.Web.csproj...
  Determining projects to restore...
  Restored C:\git\csharp\LetsShip\src\PriceFalcon.Web\PriceFalcon.Web.csproj (in 65 ms).
Restore succeeded.
```

For the next steps we're going to use Visual Studio 2019 for a full IDE experience. In order to use this we need a Solution file (`.sln`).

Still in the `src` folder we can run:

```
dotnet new sln -n PriceFalcon
```

To create an empty Solution file. It won't be linked to our web project yet though. First you'll need to install Visual Studio 2019 if you don't already have it, so we'll take a break here and commit the code so far.

## Step 2

You are now the proud owner of 1 Visual Studio 2019, congratulations. Double click the `PriceFalcon.sln` solution we created and Visual Studio 2019 should launch.

If you expand Solution Explorer you should see and empty Solution. If you right click it and select `Add > Existing Project...` then we can browse for the project (`.csproj`) file for the web project.

[IMG001][img002][IMG003]

We now have a minimal solution and web project set-up.

Before we go any further we also want a database to develop against. In order to have a reproducible and nicely contained database environment we're going to use docker images to run the database while developing.

We're following this guide to run docker with postgres locally. https://towardsdatascience.com/how-to-run-postgresql-using-docker-15bf87b452d4

First we want a directory to store our postgres container related stuff in. In the root directory of our repository (i.e. not in `src`) run:

```
mkdir db
```

We also need a directory for the data our container uses to be persisted (`data`) and scripts used to configure the database on first run (`init`):

```
cd db
mkdir data
mkdir init
```

Inside the `db/init` folder I created a new file `init.sql` with the following content to create a new user for our development purposes. This will be run when our postgres container starts up:

```
create user devwrite with encrypted password 'xE:UZj4buVy2&&3n';
grant all privileges on database pricefalcon to devwrite;
```

Then in the `db` folder I created the file `docker-compose.yml` with the following content, modified from the tutorial:

```
version: "3.8"

services:
  postgresdb:
    container_name: pg_container
    image: postgres
    restart: always
    environment:
      POSTGRES_USER: root
      POSTGRES_PASSWORD: root
      POSTGRES_DB: pricefalcon
    ports:
      - "5432:5432"
    volumes:
      - ./data:/var/lib/postgresql/data
      - ./init:/docker-entrypoint-initdb.d

volumes:
  data:
  init:

```

This creates a new postgres database docker image with the superuser username/password of root/root. The database name is `pricefalcon`, the database is available on port `5432` (postgres default). It also mounts the `data` folder so our database is persistent between restarts (automatic restart of the container enabled by `restart: always`). Lastly it mounts the `init` folder to the default folder used to run scripts when the container starts.

We should be ready to go now. In the `db` folder run:

```
docker-compose up
```

> Note: It appears docker might cache the postgres details if you've previously run postgres in a container, see this https://stackoverflow.com/a/56682187 if you have trouble.

> Another Note: YAML, the format for docker-compose is the worst ever config syntax and is super-finicky about whitespace, check you've got the whitespace exactly right if `docker-compose up` complains.

Once you have run this command you should get the output logging of the container starting up:

```
C:\git\csharp\LetsShip\db>docker-compose up
Creating network "db_default" with the default driver
Creating volume "db_data" with default driver
Creating volume "db_init" with default driver
Creating pg_container ... done
Attaching to pg_container
pg_container  | The files belonging to this database system will be owned by user "postgres".
pg_container  | This user must also own the server process.
pg_container  |
pg_container  | The database cluster will be initialized with locale "en_US.utf8".
pg_container  | The default database encoding has accordingly been set to "UTF8".
pg_container  | The default text search configuration will be set to "english".
pg_container  |
pg_container  | Data page checksums are disabled.
pg_container  |
pg_container  | fixing permissions on existing directory /var/lib/postgresql/data ... ok
pg_container  | creating subdirectories ... ok
pg_container  | selecting dynamic shared memory implementation ... posix
pg_container  | selecting default max_connections ... 100
pg_container  | selecting default shared_buffers ... 128MB
pg_container  | selecting default time zone ... Etc/UTC
pg_container  | creating configuration files ... ok
pg_container  | running bootstrap script ... ok
pg_container  | performing post-bootstrap initialization ... ok
pg_container  | syncing data to disk ... ok
pg_container  |
pg_container  |
pg_container  | Success. You can now start the database server using:
pg_container  |
pg_container  |     pg_ctl -D /var/lib/postgresql/data -l logfile start
pg_container  |
pg_container  | initdb: warning: enabling "trust" authentication for local connections
pg_container  | You can change this by editing pg_hba.conf or using the option -A, or
pg_container  | --auth-local and --auth-host, the next time you run initdb.
pg_container  | waiting for server to start....2021-03-17 20:39:21.068 UTC [47] LOG:  starting PostgreSQL 13.2 (Debian 13.2-1.pgdg100+1) on x86_64-pc-linux-gnu, compiled by gcc (Debian 8.3.0-6) 8.3.0, 64-bit
pg_container  | 2021-03-17 20:39:21.070 UTC [47] LOG:  listening on Unix socket "/var/run/postgresql/.s.PGSQL.5432"
pg_container  | 2021-03-17 20:39:21.106 UTC [48] LOG:  database system was shut down at 2021-03-17 20:39:17 UTC
pg_container  | 2021-03-17 20:39:21.138 UTC [47] LOG:  database system is ready to accept connections
pg_container  |  done
pg_container  | server started
pg_container  | CREATE DATABASE
pg_container  |
pg_container  |
pg_container  | /usr/local/bin/docker-entrypoint.sh: running /docker-entrypoint-initdb.d/init.sql
pg_container  | CREATE ROLE
pg_container  | GRANT
pg_container  |
pg_container  |
pg_container  | 2021-03-17 20:39:24.266 UTC [47] LOG:  received fast shutdown request
pg_container  | waiting for server to shut down...2021-03-17 20:39:24.270 UTC [47] LOG:  aborting any active transactions
pg_container  | 2021-03-17 20:39:24.272 UTC [47] LOG:  background worker "logical replication launcher" (PID 54) exited with exit code 1
pg_container  | 2021-03-17 20:39:24.275 UTC [49] LOG:  shutting down
pg_container  | .2021-03-17 20:39:24.412 UTC [47] LOG:  database system is shut down
pg_container  |  done
pg_container  | server stopped
pg_container  |
pg_container  | PostgreSQL init process complete; ready for start up.
pg_container  |
pg_container  | 2021-03-17 20:39:24.530 UTC [1] LOG:  starting PostgreSQL 13.2 (Debian 13.2-1.pgdg100+1) on x86_64-pc-linux-gnu, compiled by gcc (Debian 8.3.0-6) 8.3.0, 64-bit
pg_container  | 2021-03-17 20:39:24.530 UTC [1] LOG:  listening on IPv4 address "0.0.0.0", port 5432
pg_container  | 2021-03-17 20:39:24.530 UTC [1] LOG:  listening on IPv6 address "::", port 5432
pg_container  | 2021-03-17 20:39:24.540 UTC [1] LOG:  listening on Unix socket "/var/run/postgresql/.s.PGSQL.5432"
pg_container  | 2021-03-17 20:39:24.579 UTC [84] LOG:  database system was shut down at 2021-03-17 20:39:24 UTC
pg_container  | 2021-03-17 20:39:24.614 UTC [1] LOG:  database system is ready to accept connections
```

You can close the window (not using CTRL + C but actually closing the window itself from the red 'x') and the container will continue running and will start up whenever you reboot the machine.

We're ready to store and retrieve data, always a good first step for web applications.

A couple more useful tools, I like to have a script to force-restart my entire database and clear the cached data if it's completely broken.

In the `db` folder I created a `force-restart.bat` file:

```
docker-compose down
rmdir /S /Q .\data
mkdir data
docker-compose up
```

This will entirely wipe and rebuild the database container if you need a clean start.

In addition it's useful to have a tool to connect to the database, for this purpose I recommend DBeaver: https://dbeaver.com/

You can connect to our newly created database with the following settings:

[img004]

Now we've got the first part of our infrastructure and project set-up, let's commit and take a break.

## Step 3

Now we're going to start building out some features of our application to make it a little interesting.

First we need an email validation step since we don't want people spamming us with crawl requests for nonsense.

Then we want to accept the URL for crawl and have a test page to ensure the crawl is configured correctly. Finally we need the ability for the user to delete those crawls
and a background task to run the actual crawling on an interval.

### Architecture

Generally for any application you want to keep the logic in the hosting application as small as possible so that you can easily switch the hosting application between, for example, an API, a desktop app, a mobile app, an API, etc. For this purpose and others CQRS using Mediatr is generally an unbeatable pattern, unless you have very specific requirements.

Our MVC application will defer to our CQRS library for all operations. We also need code to do the web crawling for us. For saved monitoring jobs we'll want to run this out of process. When the user is creating a task we probably want to run in-process. For this reason the actually crawling code will be self contained and shared between the MVC host and the console host for the crawler agent.

The 2 main options for data access are Entity Framework Core and Dapper. Since I love raw SQL and have a difficult time with EF we'll be using Dapper and running SQL against our database. This means we need to manage migrations ourselves, for this purpose we will use Evolve.

We will use the following 4 projects for the web application part of our project:

- PriceFalcon.Web - The MVC host application, should contain very little logic, it simply exposes our endpoints, displays the UI and forms the composition root for the web application.
- PriceFalcon.App - The meat of the application logic. The requests and handlers live in here and are responsible for all our business logic (unrelated to the actual web crawling).
- PriceFalcon.Domain - The core classes with no logic for things like users, tokens, jobs, etc.
- PriceFalcon.Infrastructure - Interact with external services such as the database, email sending, metrics etc. Only interfaces are publically exposed to the other projects.

The structure as a diagram:

[IMG005]

This might seem like over-engineering at this stage, and it probably is, but it ensures a good separation of concerns and makes sure people don't start mixing all sorts of different code in a single place. It doesn't add significant overhead and future engineers will thank you.

### Getting started

First up let's install Mediatr and create the library for commands/queries. For now handlers will live next to their commands, we could always extract these in future.

I added a new .NET 5 class lib called PriceFalcon.App and installed the Mediatr 9 NuGet package to both Web and App projects. Then I installed Mediatr.Extensions.Microsoft.DependencyInjection in just the web project.

Then I created SendEmailInvite class and SendEmailInviteHandler in the App project. Add a reference to App from Web. Add this code in Startup so that all requests and handlers are picked up.

```
services.AddMediatR(typeof(SendEmailInvite).Assembly);
```

We also need a place to stick code to send emails, interact with the database etc. To this end I added the PriceFalcon.Infrastructure project and referenced it from both Web and App.

## Step 4

Dockerise our applications. Cover

Why dockerise, multi-stage builds, reduce build size, alpine linux, setup firefox for selenium, etc.

How to push to github from github actions.

Create a personal access token (PAT).

## Step 5

k3s. Cover

Provision a server from hetzner, choose a server, setup ssh and alias the ssh action. Install and configure postgres
and k3s.

Create the simplest app to check it all works:

```
apiVersion: apps/v1
kind: Deployment
metadata:
  name: mysite
  labels:
    app: mysite
spec:
  replicas: 1
  selector:
    matchLabels:
      app: mysite
  template:
    metadata:
      labels:
        app: mysite
    spec:
      containers:
        - name: mysite
          image: kellygriffin/hello:v1
          ports:
            - containerPort: 80
---
apiVersion: v1
kind: Service
metadata:
  name: mysite
spec:
  ports:
  - name: http
    targetPort: 80
    port: 80
  selector:
    app: mysite
---
apiVersion: extensions/v1beta1
kind: Ingress
metadata:
  name: mysite
  annotations:
    kubernetes.io/ingress.class: "traefik"
spec:
  rules:
  - http:
      paths:
      - path: /
        backend:
          serviceName: mysite
          servicePort: http
```

Load it in:

```
kubectl apply -f testdeploy.yaml
```

Delete it:

```
kubectl delete -f testdeploy.yaml
```

Get the running containers:

```
crictl ps
```

Get the container id and run:

```
crictl exec -it 57c8989ef713f '/bin/sh'
```

To attach with a shell.

# Notes

- https://mbuffett.com/posts/kubernetes-setup/
- https://www.youtube.com/watch?v=lAyL9HKx8cQ
