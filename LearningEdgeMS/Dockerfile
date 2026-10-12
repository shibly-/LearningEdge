# Stage 1: Build and publish the worker
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# The worker project references Messaging.Contracts two folders above its own directory.
COPY ["LearningEdge/src/LearningEdge.Messaging.Contracts/LearningEdge.Messaging.Contracts.csproj", "LearningEdge/src/LearningEdge.Messaging.Contracts/"]
COPY ["LearningEdgeMS/LearningEdgeMS.DocumentParserService/LearningEdgeMS.DocumentParserService.csproj", "LearningEdgeMS/LearningEdgeMS.DocumentParserService/"]
RUN dotnet restore "LearningEdgeMS/LearningEdgeMS.DocumentParserService/LearningEdgeMS.DocumentParserService.csproj"

COPY ["LearningEdge/src/LearningEdge.Messaging.Contracts/", "LearningEdge/src/LearningEdge.Messaging.Contracts/"]
COPY ["LearningEdgeMS/LearningEdgeMS.DocumentParserService/", "LearningEdgeMS/LearningEdgeMS.DocumentParserService/"]
RUN dotnet publish "LearningEdgeMS/LearningEdgeMS.DocumentParserService/LearningEdgeMS.DocumentParserService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime image. This is a worker, so it does not host HTTP.
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "LearningEdgeMS.DocumentParserService.dll"]
