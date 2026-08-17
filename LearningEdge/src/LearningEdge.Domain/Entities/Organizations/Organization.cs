using LearningEdge.Domain.Common;
using LearningEdge.Domain.Entities.Users;
using System.ComponentModel.DataAnnotations.Schema;

namespace LearningEdge.Domain.Entities.Organizations;

[Table("Organization", Schema = "le")]
public sealed class Organization: BaseEntity<Guid>
{
    public string Name { get; private set; } 
    public string Description { get; private set; } = string.Empty; 
    private readonly List<User> _users = new();    
    public IReadOnlyCollection<User> Users => _users.AsReadOnly();
    
    //private Organization() { }
    public Organization(string name, string description = "")
    {
        Id = Guid.NewGuid();
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description;
    }
}
