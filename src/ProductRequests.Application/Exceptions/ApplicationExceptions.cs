namespace ProductRequests.Application.Exceptions;

public abstract class CodedApplicationException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public sealed class ValidationException(string message)
    : CodedApplicationException("VALIDATION_ERROR", message);

public sealed class ResourceNotFoundException(string code, string message)
    : CodedApplicationException(code, message);

public sealed class ResourceAccessDeniedException(string message)
    : CodedApplicationException("RESOURCE_ACCESS_DENIED", message);

public sealed class AuthenticationFailureException(string code)
    : CodedApplicationException(code, "Authentication failed.");
