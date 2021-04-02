FROM mcr.microsoft.com/dotnet/sdk:5.0 as build

RUN mkdir build

WORKDIR /build

COPY src .

RUN mkdir crawler

RUN dotnet restore -v n -r linux-x64 PriceFalcon.JobRunner/PriceFalcon.JobRunner.csproj

RUN dotnet publish -v n --no-restore -c Release -r linux-x64 -o crawler PriceFalcon.JobRunner/PriceFalcon.JobRunner.csproj

FROM mcr.microsoft.com/dotnet/runtime-deps:5.0.4-focal as run

RUN apt-get update && \
  apt-get -y --no-install-recommends install \
  xvfb \
  firefox \
  libfontconfig \
  libfreetype6 \
  xfonts-cyrillic \
  xfonts-scalable \
  && rm -rf /var/lib/apt/lists/* \
  && apt-get -qyy clean

# For Xvfb (X virtual frame buffer, enables in-memory GUI without GUI installed).
ENV DISPLAY :99

# From https://github.com/SeleniumHQ/docker-selenium/blob/trunk/NodeBase/Dockerfile
ENV LANG_WHICH en
ENV LANG_WHERE US
ENV ENCODING UTF-8
ENV LANGUAGE ${LANG_WHICH}_${LANG_WHERE}.${ENCODING}
ENV LANG ${LANGUAGE}

RUN mkdir app

WORKDIR /app

COPY docker/crawler/run-via-xvfb.sh /app/run-via-xvfb.sh
RUN chmod a+x /app/run-via-xvfb.sh

COPY --from=build /build/crawler .

RUN chmod a+x Drivers/Linux/geckodriver

ENTRYPOINT [ "./run-via-xvfb.sh" ]

# docker run --rm -it -e ConnectionStrings__Default="Server=host.docker.internal;Port=5432;Database=pricefalcon;User Id=devwrite;Password=xE:UZj4buVy2&&3n;" -e DOTNET_ENVIRONMENT="Development" crawler