# Stage 1: Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /api
EXPOSE 80
EXPOSE 443

# Stage 2: Build image with SDK
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the solution file and restore dependencies
COPY ["LearningEdge.slnx", "./"]
COPY . .
RUN dotnet restore "LearningEdge.slnx"

# Stage 3: Publish the API only. OpenAPI document generation is disabled
# because it starts the app at build time and would try to connect to SQL Server.
FROM build AS publish
RUN dotnet publish "src/LearningEdge.Api/LearningEdge.Api.csproj" -c Release -o /api/publish /p:UseAppHost=false /p:OpenApiGenerateDocumentsOnBuild=false

# Stage 4: Final runtime image
FROM base AS final
WORKDIR /api
COPY --from=publish /api/publish .
ENV ASPNETCORE_ENVIRONMENT=Docker
ENV ASPNETCORE_URLS=https://+:443;http://+:80
ENTRYPOINT ["dotnet", "LearningEdge.Api.dll"]
