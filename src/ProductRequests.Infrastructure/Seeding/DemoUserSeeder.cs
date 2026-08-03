using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProductRequests.Domain.Users;
using ProductRequests.Infrastructure.Persistence;

namespace ProductRequests.Infrastructure.Seeding;

public sealed class DemoUserSeeder(
    ProductRequestsDbContext context,
    IPasswordHasher<User> passwordHasher)
{
    private const string DemoPassword = "Passw0rd!";

    private static readonly (string Name, string Email, UserRole Role)[] DemoUsers =
    [
        ("Demo Client", "client@example.com", UserRole.Client),
        ("Demo Client 2", "client2@example.com", UserRole.Client),
        ("Demo Provider 1", "provider1@example.com", UserRole.Provider),
        ("Demo Provider 2", "provider2@example.com", UserRole.Provider)
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        foreach ((string name, string email, UserRole role) in DemoUsers)
        {
            string normalizedEmail = User.NormalizeEmail(email);
            if (await context.Users.AnyAsync(
                    user => user.NormalizedEmail == normalizedEmail,
                    cancellationToken))
            {
                continue;
            }

            User placeholder = User.Create(
                Guid.NewGuid(), name, email, "pending", role, DateTimeOffset.UtcNow);
            string passwordHash = passwordHasher.HashPassword(placeholder, DemoPassword);
            User user = User.Create(placeholder.Id, name, email, passwordHash, role, placeholder.CreatedAt);
            context.Users.Add(user);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
