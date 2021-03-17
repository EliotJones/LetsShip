# Let's Ship - PriceFalcon #

The aim of this repository is to share the experience of developing and shipping a working .NET 5 application including deployments, dev-ops, migrations, etc.

This will be a learning experience for me and hopefully the write up will help people get up to speed with .NET 5 if they've never used it before, including deploying to Linux machines.

The application we're building is an MVC web application backed by a PostgresSQL data store. The application allows users to submit arbitrary URLs of product pages from other sites to monitor for price changes and receive email alerts.

## Step 1 (you say we need to talk) ##

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