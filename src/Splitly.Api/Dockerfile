# Build from the repository root: docker build -f src/Splitly.Api/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Restore first so the package layer is cached until a project file changes.
COPY global.json nuget.config Directory.Build.props Directory.Packages.props ./
COPY src/Splitly.Domain/Splitly.Domain.csproj src/Splitly.Domain/
COPY src/Splitly.Api/Splitly.Api.csproj src/Splitly.Api/
RUN dotnet restore src/Splitly.Api/Splitly.Api.csproj

COPY src/ src/
RUN dotnet publish src/Splitly.Api/Splitly.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID

ENTRYPOINT ["dotnet", "Splitly.Api.dll"]
