namespace LearningEdge.Application.Caching;

public static class FileExtractCache
{
    public static readonly TimeSpan StatusLifetime = TimeSpan.FromHours(1);

    public static string StatusKey(Guid fileId) => $"file_extraction:{fileId}";

    public static string StatusValueKey(Guid fileId) => $"{StatusKey(fileId)}:status";

    public static string ProgressKey(Guid fileId) => $"{StatusKey(fileId)}:progress";

    public static class Status
    {
        public const string Queued = "Queued";
        public const string Processing = "Processing";
        public const string Completed = "Completed";
        public const string Failed = "Failed";
    }
}
