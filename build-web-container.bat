RD /S /Q src\PriceFalcon.Web\bin
RD /S /Q src\PriceFalcon.Web\obj
RD /S /Q src\PriceFalcon.App\bin
RD /S /Q src\PriceFalcon.App\obj
RD /S /Q src\PriceFalcon.Infrastructure\bin
RD /S /Q src\PriceFalcon.Infrastructure\obj

docker build -t falconweb:latest -f docker/web/Dockerfile.yml .