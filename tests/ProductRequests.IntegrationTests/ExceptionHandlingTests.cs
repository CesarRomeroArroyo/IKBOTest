using Microsoft.EntityFrameworkCore;
using ProductRequests.Api.ExceptionHandling;
using ProductRequests.Application.Exceptions;
using ProductRequests.Domain.Exceptions;

namespace ProductRequests.IntegrationTests;

public sealed class ExceptionHandlingTests
{
    [Theory]
    [MemberData(nameof(KnownErrors))]
    public void KnownErrorsMapToStableProblemCodes(Exception exception, int status, string code)
    {
        ExceptionDescriptor descriptor = ExceptionDescriptor.From(exception);

        Assert.Equal(status, descriptor.Status);
        Assert.Equal(code, descriptor.Code);
    }

    [Fact]
    public void UnexpectedErrorDoesNotExposeStackOrMessage()
    {
        var exception = new InvalidOperationException("secret internal detail");

        ExceptionDescriptor descriptor = ExceptionDescriptor.From(exception);

        Assert.Equal(500, descriptor.Status);
        Assert.Equal("UNEXPECTED_ERROR", descriptor.Code);
        Assert.DoesNotContain("secret", descriptor.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(nameof(InvalidOperationException), descriptor.Detail, StringComparison.Ordinal);
    }

    public static TheoryData<Exception, int, string> KnownErrors() => new()
    {
        { new ValidationException("invalid"), 400, "VALIDATION_ERROR" },
        { new ResourceNotFoundException("OFFER_NOT_FOUND", "missing"), 404, "OFFER_NOT_FOUND" },
        { new ResourceAccessDeniedException("denied"), 403, "RESOURCE_ACCESS_DENIED" },
        { new DomainException("REQUEST_ALREADY_AWARDED", "awarded"), 409, "REQUEST_ALREADY_AWARDED" },
        { new DbUpdateConcurrencyException("conflict"), 409, "CONCURRENCY_CONFLICT" },
        { new AuthenticationFailureException("INVALID_CREDENTIALS"), 401, "INVALID_CREDENTIALS" }
    };
}
