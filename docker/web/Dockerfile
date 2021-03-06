FROM mcr.microsoft.com/dotnet/sdk:5.0-alpine3.13 as build
LABEL stage=builder

WORKDIR /build

COPY src .

# dotnet restore is a hefty boi, if it is run separately it can be cached.
# linux-musl-x64 is for lightweight containers like alpine linux.
RUN dotnet restore -v n -r linux-musl-x64 PriceFalcon.Web/PriceFalcon.Web.csproj

# Run the publish targeting the same runtime but with no restore.
RUN dotnet publish -v n --no-restore -c Release -r linux-musl-x64 -o web PriceFalcon.Web/PriceFalcon.Web.csproj

FROM mcr.microsoft.com/dotnet/runtime-deps:5.0.4-alpine3.13 as run

# Create a user called "app" who has ownership of the "app" directory.
RUN adduser --disabled-password --home /app --gecos '' app && chown -R app /app

USER app

WORKDIR /app

COPY --from=build /build/web .

ENV ASPNETCORE_URLS=http://+:6110 \
    SiteUrl=http://localhost:6110

EXPOSE 6110

ENTRYPOINT [ "./PriceFalcon.Web" ]

# If I want to run it up locally for testing (Docker for Windows or Mac)
# docker run --rm -it -p 6110:6110 -e ConnectionStrings__Default="Server=host.docker.internal;Port=5432;Database=pricefalcon;User Id=devwrite;Password=xE:UZj4buVy2&&3n;" -e ASPNETCORE_ENVIRONMENT="Development" -e PRICE_FALCON_SENDGRID="SG.fakeapikey" --entrypoint /bin/ash falconweb