FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY WattWeather.slnx ./
COPY app/WattWeather.App.csproj app/
COPY server/WattWeather.Server.csproj server/
COPY tests/WattWeather.Tests.csproj tests/
RUN dotnet restore WattWeather.slnx

COPY app/ app/
COPY server/ server/
COPY tests/ tests/
RUN dotnet test WattWeather.slnx -c Release --no-restore
RUN dotnet publish server/WattWeather.Server.csproj -c Release --no-restore -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "WattWeather.Server.dll"]
