using LearningEdge.Domain.Common;
using LearningEdge.Domain.Entities.Exams;

namespace LearningEdge.Domain.Entities.Trainings;

public sealed class Training : BaseEntity<Guid>
{
    public Guid CategoryId { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    private readonly List<TrainingFile> _files = new();
    public IReadOnlyCollection<TrainingFile> Files => _files.AsReadOnly();

    private readonly List<Exam> _exams = new();
    public IReadOnlyCollection<Exam> Exams => _exams.AsReadOnly();

    public Training(Guid categoryId, string name, string description = "", bool isActive = true)
    {
        Id = Guid.NewGuid();
        CategoryId = categoryId;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? string.Empty;
        IsActive = isActive;
    }

    public void Update(string name, string description, bool isActive)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? string.Empty;
        IsActive = isActive;
        MarkUpdated();
    }

    public TrainingFile AddFile(string fileName, string contentType, long sizeBytes, string storageKey, Guid uploadedByUserId)
    {
        var file = new TrainingFile(Id, fileName, contentType, sizeBytes, storageKey, uploadedByUserId);
        _files.Add(file);
        return file;
    }

    /// <summary>
    /// Soft-deletes the file so it drops out of queries. The row stays on <c>TrainingFile</c>.
    /// Returns null when this training does not have that file.
    /// </summary>
    public TrainingFile? RemoveFile(Guid fileId)
    {
        var file = _files.FirstOrDefault(candidate => candidate.Id == fileId);
        if (file is null)
        {
            return null;
        }

        file.SoftDelete();
        return file;
    }

    public TrainingFile? FileExtractionIsInitiated(Guid fileId)
    {
        var file = _files.FirstOrDefault(candidate => candidate.Id == fileId);
        if (file is null)
        {
            return null;
        }

        file.MarkAsExtractionProcessStarted();
        MarkUpdated();
        return file;
    }

    public TrainingFile? FileExtractionIsCompleted(Guid fileId)
    {
        var file = _files.FirstOrDefault(candidate => candidate.Id == fileId);
        if (file is null)
        {
            return null;
        }

        file.MarkAsExtracted();
        MarkUpdated();
        return file;
    }

    public TrainingFile? MCQGenerationIsInitiated(Guid fileId)
    {
        var file = _files.FirstOrDefault(candidate => candidate.Id == fileId);
        if (file is null)
        {
            return null;
        }

        file.MarkAsMCQGenerationProcessStarted();
        MarkUpdated();
        return file;
    }

    public TrainingFile? MCQGenerationIsCompleted(Guid fileId)
    {
        var file = _files.FirstOrDefault(candidate => candidate.Id == fileId);
        if (file is null)
        {
            return null;
        }

        file.MarkAsMCQGenerated();
        MarkUpdated();
        return file;
    }

    public void AddExam(Exam exam) => _exams.Add(exam);

    public Exam? RemoveExam(Guid examId)
    {
        var exam = _exams.FirstOrDefault(candidate => candidate.Id == examId);
        if (exam is null)
        {
            return null;
        }
        exam.SoftDelete();
        return exam;
    }
}
