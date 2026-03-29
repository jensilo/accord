FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

COPY src/Accord.Web/Accord.Web.csproj src/Accord.Web/
RUN dotnet restore src/Accord.Web/Accord.Web.csproj

COPY src/Accord.Web/ src/Accord.Web/
COPY docs/ docs/

RUN dotnet publish src/Accord.Web/Accord.Web.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app .
COPY --from=build /source/docs ./docs

RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Accord.Web.dll"]
