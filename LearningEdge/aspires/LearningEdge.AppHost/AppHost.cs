var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.LearningEdge_Api>("api");

builder.Build().Run();
