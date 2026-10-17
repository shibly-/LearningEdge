namespace LearningEdge.Api.Hubs;

public sealed record FileExtractProgressUpdate(Guid FileId, string Status, int Progress);
