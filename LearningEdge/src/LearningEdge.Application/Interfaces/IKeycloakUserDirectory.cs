namespace LearningEdge.Application.Interfaces;

/// <summary>The account the admin API creates in Keycloak.</summary>
public sealed record KeycloakAccount(
    string Email,
    string FirstName,
    string LastName,
    string Password);

/// <summary>
/// Creates an enabled realm user. Implemented with the Keycloak admin REST API
/// so the browser never talks to that API directly.
/// </summary>
public interface IKeycloakUserDirectory
{
    /// <summary>Returns the Keycloak user id. Assigns the configured learner role.</summary>
    Task<string> CreateLearnerAsync(KeycloakAccount account, CancellationToken cancellationToken);

    /// <summary>Returns the Keycloak user id. Assigns the configured organization-admin role.</summary>
    Task<string> CreateOrganizationAdminAsync(KeycloakAccount account, CancellationToken cancellationToken);

    /// <summary>Turns the realm user's sign-in on or off without changing their password.</summary>
    Task SetEnabledAsync(string keycloakUserId, bool enabled, CancellationToken cancellationToken);

    /// <summary>Keeps the realm user's name and email aligned with the LearningEdge record.</summary>
    Task UpdateProfileAsync(
        string keycloakUserId,
        string email,
        string firstName,
        string lastName,
        CancellationToken cancellationToken);
}
