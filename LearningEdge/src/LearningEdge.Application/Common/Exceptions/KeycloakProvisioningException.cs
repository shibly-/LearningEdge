namespace LearningEdge.Application.Common.Exceptions;

public sealed class KeycloakProvisioningException : Exception
{
    public KeycloakProvisioningException(string message) : base(message)
    {
    }

    public KeycloakProvisioningException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
