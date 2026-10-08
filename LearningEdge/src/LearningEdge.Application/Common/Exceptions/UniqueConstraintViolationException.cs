namespace LearningEdge.Application.Common.Exceptions;

public sealed class UniqueConstraintViolationException(string message, Exception innerException)
    : Exception(message, innerException);
