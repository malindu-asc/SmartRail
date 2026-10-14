# Build context: repository root.   docker build -f src/Host/SmartRail.Api/Dockerfile -t smartrail-api .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY src ./src

RUN dotnet publish src/Host/SmartRail.Api/SmartRail.Api.csproj \
    -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true
EXPOSE 8080

COPY --from=build /app/publish .

# The aspnet image ships a non-root "app" user.
USER $APP_UID
ENTRYPOINT ["dotnet", "SmartRail.Api.dll"]
