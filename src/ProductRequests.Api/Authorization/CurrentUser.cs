using System.Security.Claims;
using ProductRequests.Application.Abstractions;
using ProductRequests.Application.Exceptions;
using ProductRequests.Domain.Users;

namespace ProductRequests.Api.Authorization;

internal sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal Principal => accessor.HttpContext?.User
        ?? throw new AuthenticationFailureException("INVALID_CREDENTIALS");

    public Guid Id
    {
        get
        {
            string? subject = Principal.FindFirst("sub")?.Value;
            if (!Guid.TryParse(subject, out Guid id))
            {
                throw new AuthenticationFailureException("INVALID_CREDENTIALS");
            }

            return id;
        }
    }

    public string Email => Principal.FindFirst("email")?.Value
        ?? throw new AuthenticationFailureException("INVALID_CREDENTIALS");

    public UserRole Role
    {
        get
        {
            string? role = Principal.FindFirst("role")?.Value;
            if (!Enum.TryParse(role, true, out UserRole userRole))
            {
                throw new AuthenticationFailureException("INVALID_CREDENTIALS");
            }

            return userRole;
        }
    }
}
