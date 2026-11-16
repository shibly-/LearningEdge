namespace LearningEdge.Application.Interfaces;

/// <summary>
/// Reversible protection for a password that must be sent to Keycloak later.
/// The plaintext never stays in the database.
/// </summary>
public interface IPendingCredentialProtector
{
    string Protect(string password);

    string Unprotect(string protectedPassword);
}
