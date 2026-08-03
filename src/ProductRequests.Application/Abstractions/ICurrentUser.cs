using ProductRequests.Domain.Users;

namespace ProductRequests.Application.Abstractions;

public interface ICurrentUser
{
    Guid Id { get; }
    string Email { get; }
    UserRole Role { get; }
}
