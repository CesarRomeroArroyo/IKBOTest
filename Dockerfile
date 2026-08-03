FROM mcr.microsoft.com/dotnet/sdk:8.0.403 AS build
WORKDIR /src

COPY global.json Directory.Build.props ProductRequests.sln ./
COPY src/ProductRequests.Api/ProductRequests.Api.csproj src/ProductRequests.Api/
COPY src/ProductRequests.Application/ProductRequests.Application.csproj src/ProductRequests.Application/
COPY src/ProductRequests.Domain/ProductRequests.Domain.csproj src/ProductRequests.Domain/
COPY src/ProductRequests.Infrastructure/ProductRequests.Infrastructure.csproj src/ProductRequests.Infrastructure/
COPY tests/ProductRequests.Domain.Tests/ProductRequests.Domain.Tests.csproj tests/ProductRequests.Domain.Tests/
COPY tests/ProductRequests.IntegrationTests/ProductRequests.IntegrationTests.csproj tests/ProductRequests.IntegrationTests/
RUN test "$(dotnet --version)" = "8.0.403" && dotnet restore ProductRequests.sln

COPY . .
RUN dotnet publish src/ProductRequests.Api/ProductRequests.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0.10 AS runtime
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
HEALTHCHECK --interval=10s --timeout=5s --retries=5 \
    CMD curl --fail --silent http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "ProductRequests.Api.dll"]
