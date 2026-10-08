using LearningEdge.Domain.Common;
using LearningEdge.Domain.Entities.Trainings;

namespace LearningEdge.Domain.Entities.Categories;

public sealed class Category : BaseEntity<Guid>
{
    public Guid OrganizationId { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    private readonly List<Training> _trainings = new();
    public IReadOnlyCollection<Training> Trainings => _trainings.AsReadOnly();

    public Category(Guid organizationId, string name, string description = "", bool isActive = true)
    {
        Id = Guid.NewGuid();
        OrganizationId = organizationId;
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
}
