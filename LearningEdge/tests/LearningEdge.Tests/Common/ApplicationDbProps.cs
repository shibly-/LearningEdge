using LearningEdge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace LearningEdge.Tests.Common;

public static class ApplicationDbProps
{
    public static string getDbConnectionString() {

        // Navigate to API project folder
        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "../LearningEdge.Api");

        // Build configuration from appsettings.json in API project
        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();
        var connectionString = configuration.GetConnectionString("LearningEdgeDBConnectionDockerDev");
        return connectionString ?? string.Empty;
    }    
}
