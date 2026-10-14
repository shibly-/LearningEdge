using LearningEdge.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;
using System.Xml.Linq;

namespace LearningEdge.Domain.Entities.Trainings;

[Table("TrainingFile", Schema = "le")]
public sealed class TrainingFile : BaseEntity<Guid>
{
    public Guid TrainingId { get; private set; }
    public string FileName { get; private set; }
    public string ContentType { get; private set; }
    public long SizeBytes { get; private set; }
    public string StorageKey { get; private set; }
    public Guid UploadedByUserId { get; private set; }
    public bool IsFileExtracted { get; private set; }
    public bool IsMCQGenerated { get; private set; }
    public bool IsFileExtractionRequested { get; private set; }
    public bool IsMCQGenerationRequested { get; private set; }
    public bool IsFileExtractionStarted { get; private set; }
    public bool IsMCQGenerationStarted { get; private set; }
    public bool IsFileExtractionFailed { get; private set; }
    public bool IsMCQGenerationFailed { get; private set; }
    public string FileExtractionFailureMessage { get; private set; } = string.Empty;
    public string MCQGenerationFailureMessage { get; private set; } = string.Empty;

    internal TrainingFile(Guid trainingId, string fileName, string contentType, long sizeBytes, string storageKey, Guid uploadedByUserId)
    {
        Id = Guid.NewGuid();
        TrainingId = trainingId;
        FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
        ContentType = contentType ?? throw new ArgumentNullException(nameof(contentType));
        SizeBytes = sizeBytes;
        StorageKey = storageKey ?? throw new ArgumentNullException(nameof(storageKey));
        UploadedByUserId = uploadedByUserId;
        IsFileExtracted = false;
        IsMCQGenerated = false;
        IsFileExtractionRequested = false;
        IsMCQGenerationRequested = false;
        IsFileExtractionStarted = false;
        IsMCQGenerationStarted = false;
        IsFileExtractionFailed = false;
        IsMCQGenerationFailed = false;
    }

    public void MarkAsExtractionProcessStarted()
    {
        IsFileExtractionRequested = true;

        // reset other fields
        IsFileExtractionStarted = true;
        IsFileExtractionFailed = false;
        IsMCQGenerated = false;        
        IsMCQGenerationStarted = false;
        IsMCQGenerationFailed = false;

        FileExtractionFailureMessage = string.Empty;
        MCQGenerationFailureMessage = string.Empty;
    }

    public void MarkAsMCQGenerationProcessStarted()
    {
        IsMCQGenerationRequested = true;

        // reset other fields        
        IsMCQGenerationStarted = false;
        IsMCQGenerated = false;
        IsMCQGenerationFailed = false;

        MCQGenerationFailureMessage = string.Empty;
    }

    public void MarkAsExtracted()
    {
        IsFileExtracted = true;
    }

    public void MarkAsMCQGenerated()
    {   
        IsMCQGenerated = true;
    }
}
