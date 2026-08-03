using Microsoft.AspNetCore.Identity;
using ProductRequests.Application.Auth;
using ProductRequests.Domain.Users;

namespace ProductRequests.Infrastructure.Authentication;

internal sealed class PasswordVerifier(IPasswordHasher<User> passwordHasher) : IPasswordVerifier
{
    public bool Verify(User user, string password) =>
        passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password) is
            PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
}
