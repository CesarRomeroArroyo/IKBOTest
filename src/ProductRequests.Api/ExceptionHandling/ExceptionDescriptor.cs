using Microsoft.EntityFrameworkCore;
using ProductRequests.Application.Exceptions;
using ProductRequests.Domain.Exceptions;

namespace ProductRequests.Api.ExceptionHandling;

public sealed record ExceptionDescriptor(int Status, string Code, string Title, string Detail)
{
    private static readonly HashSet<string> ValidationCodes =
    [
        "VALIDATION_ERROR",
        "INVALID_AMOUNT",
        "INVALID_QUANTITY",
        "INVALID_DELIVERY_DAYS"
    ];

    private static readonly HashSet<string> NotFoundCodes =
    [
        "PRODUCT_REQUEST_NOT_FOUND",
        "OFFER_NOT_FOUND"
    ];

    public static ExceptionDescriptor From(Exception exception)
    {
        if (exception is AuthenticationFailureException authentication)
        {
            return Create(401, authentication.Code, "Unauthorized", authentication.Message);
        }

        if (exception is ValidationException validation)
        {
            return Create(400, validation.Code, "Validation failed", validation.Message);
        }

        if (exception is ResourceNotFoundException notFound)
        {
            return Create(404, notFound.Code, "Resource not found", notFound.Message);
        }

        if (exception is ResourceAccessDeniedException denied)
        {
            return Create(403, denied.Code, "Access denied", denied.Message);
        }

        if (exception is DbUpdateConcurrencyException)
        {
            return Create(409, "CONCURRENCY_CONFLICT", "Concurrency conflict",
                "Resource changed while the operation was being processed.");
        }

        if (exception is DbUpdateException updateException && IsDuplicateProviderOffer(updateException))
        {
            return Create(409, "DUPLICATE_PROVIDER_OFFER", "Duplicate provider offer",
                "Provider already submitted an offer for this request.");
        }

        if (exception is DomainException domain)
        {
            if (ValidationCodes.Contains(domain.Code))
            {
                return Create(400, domain.Code, "Validation failed", domain.Message);
            }

            if (NotFoundCodes.Contains(domain.Code))
            {
                return Create(404, domain.Code, "Resource not found", domain.Message);
            }

            return Create(409, domain.Code, "Business conflict", domain.Message);
        }

        return Create(500, "UNEXPECTED_ERROR", "Unexpected error",
            "An unexpected error occurred.");
    }

    private static ExceptionDescriptor Create(int status, string code, string title, string detail) =>
        new(status, code, title, detail);

    private static bool IsDuplicateProviderOffer(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            Type type = current.GetType();
            object? number = type.GetProperty("Number")?.GetValue(current);
            bool isDuplicateKey = number is int errorNumber && errorNumber == 1062;
            if (isDuplicateKey && current.Message.Contains(
                    "UX_Offers_ProductRequestId_ProviderId",
                    StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
