# Multi-stage build for DiagnosticExplorer service + Angular SPA.
# Build context must be the repo root (see Docker/compose-*.yaml).

# Stage 1 — build the Angular SPA.
# Angular 21 requires an active Node LTS; pin to 22-alpine for repeatability.
FROM node:22-alpine AS spa-build
WORKDIR /spa
# .npmrc carries legacy-peer-deps=true, needed for npm ci to resolve the graph.
COPY diagnostics-web/package.json diagnostics-web/package-lock.json diagnostics-web/.npmrc ./
RUN npm ci
COPY diagnostics-web/ ./
RUN npm run build

# Stage 2 — build the .NET service.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["DiagnosticService/Diagnostic.Service.csproj", "DiagnosticService/"]
COPY ["DiagnosticExplorer.Hosting/DiagnosticExplorer.Hosting.csproj", "DiagnosticExplorer.Hosting/"]
COPY ["DiagnosticExplorer/DiagnosticExplorer.csproj", "DiagnosticExplorer/"]
RUN dotnet restore "DiagnosticService/Diagnostic.Service.csproj"
COPY . .
RUN dotnet publish "DiagnosticService/Diagnostic.Service.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false

# Stage 3 — runtime image.
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish ./
COPY --from=spa-build /spa/dist/diagnostics-web ./diagnostics-web
USER $APP_UID
EXPOSE 2803
ENTRYPOINT ["dotnet", "DiagnosticExplorer.Service.dll"]
