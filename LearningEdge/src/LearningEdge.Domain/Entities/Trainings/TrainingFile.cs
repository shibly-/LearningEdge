using LearningEdge.Domain.Common;

namespace LearningEdge.Domain.Entities.Trainings;

public sealed class TrainingFile : BaseEntity<Guid>
{
    public Guid TrainingId { get; private set; }
    public string FileName { get; private set; }
    public string ContentType { get; private set; }
    public long SizeBytes { get; private set; }
    public string StorageKey { get; private set; }
    public Guid UploadedByUserId { get; private set; }

    internal TrainingFile(Guid trainingId, string fileName, string contentType, long sizeBytes, string storageKey, Guid uploadedByUserId)
    {
        Id = Guid.NewGuid();
        TrainingId = trainingId;
        FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
        ContentType = contentType ?? throw new ArgumentNullException(nameof(contentType));
        SizeBytes = sizeBytes;
        StorageKey = storageKey ?? throw new ArgumentNullException(nameof(storageKey));
        UploadedByUserId = uploadedByUserId;
    }
}
