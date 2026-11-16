namespace LearningEdge.Application.Interfaces;

/// <summary>The account the admin API creates in Keycloak when a registration is approved.</summary>
public sealed record KeycloakLearnerAccount(
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
    /// <summary>Returns the Keycloak user id.</summary>
    Task<string> CreateLearnerAsync(KeycloakLearnerAccount account, CancellationToken cancellationToken);
}
