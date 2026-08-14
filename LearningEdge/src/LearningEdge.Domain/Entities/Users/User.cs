using LearningEdge.Domain.Common;
using LearningEdge.Domain.Entities.Organizations;
using LearningEdge.Domain.ValueObjects;
using System.ComponentModel.DataAnnotations.Schema;
using static LearningEdge.Domain.Common.Enums;

namespace LearningEdge.Domain.Entities.Users;

[Table("User", Schema = "le")]
public class User: BaseEntity<Guid>
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }    
    public UserRole Role { get; set; }
    public Guid OrganizationId { get; set; }

    public User(string firstName, string lastName, string email, UserRole role, Guid organizationId)
    {
        Id = Guid.NewGuid();
        FirstName = firstName ?? throw new ArgumentNullException(nameof(firstName));
        LastName = lastName ?? String.Empty;
        Email = email ?? throw new ArgumentNullException(nameof(email));
        Role = Enum.IsDefined(typeof(UserRole), role) ? role : UserRole.Learner;
        OrganizationId = organizationId;
    }
}
