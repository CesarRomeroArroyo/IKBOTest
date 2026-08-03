using Microsoft.EntityFrameworkCore;
using ProductRequests.Application.Abstractions;
using ProductRequests.Domain.Users;
using ProductRequests.Infrastructure.Persistence;

namespace ProductRequests.Infrastructure.Repositories;

internal sealed class UserRepository(ProductRequestsDbContext context) : IUserRepository
{
    public Task<User?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        context.Users.SingleOrDefaultAsync(
            user => user.NormalizedEmail == normalizedEmail,
            cancellationToken);

    public Task<bool> ExistsByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        context.Users.AnyAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken);

    public void Add(User user) => context.Users.Add(user);
}
