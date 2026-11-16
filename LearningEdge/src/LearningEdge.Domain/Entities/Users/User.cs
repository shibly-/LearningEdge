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
    public UserActivationStatus ActivationStatus { get; private set; }

    /// <summary>Data-protection ciphertext. Cleared once Keycloak has the password.</summary>
    public string? PendingPasswordProtected { get; private set; }

    public string? KeycloakUserId { get; private set; }

    public User(string firstName, string lastName, string email, UserRole role, Guid organizationId)
    {
        Id = Guid.NewGuid();
        FirstName = firstName ?? throw new ArgumentNullException(nameof(firstName));
        LastName = lastName ?? String.Empty;
        Email = email ?? throw new ArgumentNullException(nameof(email));
        Role = Enum.IsDefined(typeof(UserRole), role) ? role : UserRole.Learner;
        OrganizationId = organizationId;
        ActivationStatus = UserActivationStatus.Active;
    }

    /// <summary>
    /// A public registration. Always a learner, and inactive until
    /// <see cref="MarkApproved"/> runs.
    /// </summary>
    public static User RegisterLearner(string firstName, string lastName, string email, Guid organizationId)
    {
        var user = new User(firstName, lastName, email, UserRole.Learner, organizationId);
        user.ActivationStatus = UserActivationStatus.Pending;
        return user;
    }

    public void HoldPendingPassword(string protectedPassword)
    {
        if (ActivationStatus != UserActivationStatus.Pending)
        {
            throw new InvalidOperationException("A password can be held only for a pending registration.");
        }

        if (string.IsNullOrWhiteSpace(protectedPassword))
        {
            throw new ArgumentException("Protected password is required.", nameof(protectedPassword));
        }

        PendingPasswordProtected = protectedPassword;
    }

    public void MarkApproved(string keycloakUserId)
    {
        if (ActivationStatus != UserActivationStatus.Pending)
        {
            throw new InvalidOperationException("Only a pending user can be approved.");
        }

        if (string.IsNullOrWhiteSpace(keycloakUserId))
        {
            throw new ArgumentException("Keycloak user id is required.", nameof(keycloakUserId));
        }

        ActivationStatus = UserActivationStatus.Active;
        KeycloakUserId = keycloakUserId;
        PendingPasswordProtected = null;
        MarkUpdated();
    }

    public void Update(string firstName, string lastName, string email, UserRole role, Guid organizationId)
    {
        FirstName = firstName ?? throw new ArgumentNullException(nameof(firstName));
        LastName = lastName ?? string.Empty;
        Email = email ?? throw new ArgumentNullException(nameof(email));
        Role = Enum.IsDefined(typeof(UserRole), role) ? role : UserRole.Learner;
        OrganizationId = organizationId;
        MarkUpdated();
    }
}
