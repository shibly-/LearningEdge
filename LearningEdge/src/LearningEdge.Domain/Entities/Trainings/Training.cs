using LearningEdge.Domain.Common;

namespace LearningEdge.Domain.Entities.Trainings;

public sealed class Training : BaseEntity<Guid>
{
    public Guid CategoryId { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    private readonly List<TrainingFile> _files = new();
    public IReadOnlyCollection<TrainingFile> Files => _files.AsReadOnly();

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
}
