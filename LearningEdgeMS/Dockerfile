# Stage 1: Build and publish the worker
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Central package versions apply to projects under LearningEdge/.
COPY ["LearningEdge/Directory.Build.props", "LearningEdge/"]
COPY ["LearningEdge/Directory.Packages.props", "LearningEdge/"]
COPY ["LearningEdge/src/LearningEdge.Domain/LearningEdge.Domain.csproj", "LearningEdge/src/LearningEdge.Domain/"]
COPY ["LearningEdge/src/LearningEdge.Messaging.Contracts/LearningEdge.Messaging.Contracts.csproj", "LearningEdge/src/LearningEdge.Messaging.Contracts/"]
COPY ["LearningEdge/src/LearningEdge.Application/LearningEdge.Application.csproj", "LearningEdge/src/LearningEdge.Application/"]
COPY ["LearningEdge/src/LearningEdge.Infrastructure/LearningEdge.Infrastructure.csproj", "LearningEdge/src/LearningEdge.Infrastructure/"]
COPY ["LearningEdgeMS/LearningEdgeMS.DocumentParserService/LearningEdgeMS.DocumentParserService.csproj", "LearningEdgeMS/LearningEdgeMS.DocumentParserService/"]
RUN dotnet restore "LearningEdgeMS/LearningEdgeMS.DocumentParserService/LearningEdgeMS.DocumentParserService.csproj"

COPY ["LearningEdge/src/LearningEdge.Domain/", "LearningEdge/src/LearningEdge.Domain/"]
COPY ["LearningEdge/src/LearningEdge.Messaging.Contracts/", "LearningEdge/src/LearningEdge.Messaging.Contracts/"]
COPY ["LearningEdge/src/LearningEdge.Application/", "LearningEdge/src/LearningEdge.Application/"]
COPY ["LearningEdge/src/LearningEdge.Infrastructure/", "LearningEdge/src/LearningEdge.Infrastructure/"]
COPY ["LearningEdgeMS/LearningEdgeMS.DocumentParserService/", "LearningEdgeMS/LearningEdgeMS.DocumentParserService/"]
RUN dotnet publish "LearningEdgeMS/LearningEdgeMS.DocumentParserService/LearningEdgeMS.DocumentParserService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime image. This is a worker, so it does not host HTTP.
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "LearningEdgeMS.DocumentParserService.dll"]
