# Build context: repository root.   docker build -f tools/Simulator/SmartRail.Simulator/Dockerfile -t smartrail-simulator .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY src ./src
COPY tools/Simulator ./tools/Simulator

RUN dotnet publish tools/Simulator/SmartRail.Simulator/SmartRail.Simulator.csproj \
    -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "SmartRail.Simulator.dll"]
