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

    /// <summary>
    /// An organization admin appointed by a system admin. Already active,
    /// because Keycloak received the account before this row was saved.
    /// </summary>
    public static User CreateOrganizationAdmin(
        string firstName,
        string lastName,
        string email,
        Guid organizationId,
        string keycloakUserId)
    {
        if (string.IsNullOrWhiteSpace(keycloakUserId))
        {
            throw new ArgumentException("Keycloak user id is required.", nameof(keycloakUserId));
        }

        var user = new User(firstName, lastName, email, UserRole.OrgAdmin, organizationId);
        user.KeycloakUserId = keycloakUserId;
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

    /// <summary>
    /// Turns an organization admin's sign-in on or off. Pending registrations
    /// are approved instead, and other roles are not managed this way.
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        if (Role != UserRole.OrgAdmin)
        {
            throw new InvalidOperationException("Only an organization admin can be enabled or disabled here.");
        }

        if (ActivationStatus == UserActivationStatus.Pending)
        {
            throw new InvalidOperationException("Approve a pending account before enabling or disabling it.");
        }

        ActivationStatus = enabled ? UserActivationStatus.Active : UserActivationStatus.Inactive;
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
