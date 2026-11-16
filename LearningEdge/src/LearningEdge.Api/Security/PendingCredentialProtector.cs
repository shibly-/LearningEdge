using LearningEdge.Application.Interfaces;
using Microsoft.AspNetCore.DataProtection;

namespace LearningEdge.Api.Security;

public sealed class PendingCredentialProtector : IPendingCredentialProtector
{
    private readonly IDataProtector _protector;

    public PendingCredentialProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("LearningEdge.PendingLearnerPassword.v1");
    }

    public string Protect(string password) => _protector.Protect(password);

    public string Unprotect(string protectedPassword) => _protector.Unprotect(protectedPassword);
}
