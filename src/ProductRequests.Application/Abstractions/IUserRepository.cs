using ProductRequests.Domain.Users;

namespace ProductRequests.Application.Abstractions;

public interface IUserRepository
{
    Task<User?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken);
    Task<bool> ExistsByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken);
    void Add(User user);
}
