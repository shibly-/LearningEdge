namespace LearningEdge.Api.Constants;

public static class DefaultConfigs
{
    public const string CorsPolicyName = "AllowWebApp";
    public const string CorsOrigin = "http://localhost:4200";
    public const string RateLimiterTokenBucketPolicyName = "api";
    public const string FileStorageRootPath = "uploads";
    public const string RedisConnectionString = "127.0.0.1:6380";
    public const string RabbitMQDefaultHost = "localhost";
    public const string RabbitMQDefaultUser = "guest";
    public const string RabbitMQDefaultPassword = "guest";
    public const string MigrationsAssembly = "MigrationsAssembly";
    public const string DefaultConnectionExceptionMessage = "Connection string 'DefaultConnection' is not configured.";            
}
