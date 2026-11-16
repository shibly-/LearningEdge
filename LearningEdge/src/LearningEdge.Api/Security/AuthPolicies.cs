namespace LearningEdge.Api.Security;

public static class AuthPolicies
{
    /// <summary>
    /// Keycloak realm role for a platform operator. Matching is
    /// case-insensitive and also accepts the common aliases in
    /// <see cref="KeycloakRoles"/>.
    /// </summary>
    public const string SysAdmin = "SysAdmin";
}
