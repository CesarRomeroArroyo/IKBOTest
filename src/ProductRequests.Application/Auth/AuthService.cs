using ProductRequests.Application.Abstractions;
using ProductRequests.Domain.Users;

namespace ProductRequests.Application.Auth;

public sealed class AuthService(
    IUserRepository users,
    IPasswordVerifier passwordVerifier,
    ITokenGenerator tokenGenerator)
{
    public async Task<LoginResult> LoginAsync(
        string? email,
        string? password,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return LoginResult.Failed("INVALID_CREDENTIALS");
        }

        User? user = await users.GetByNormalizedEmailAsync(User.NormalizeEmail(email), cancellationToken);
        if (user is null || !passwordVerifier.Verify(user, password))
        {
            return LoginResult.Failed("INVALID_CREDENTIALS");
        }

        if (!user.IsActive)
        {
            return LoginResult.Failed("USER_INACTIVE");
        }

        TokenResult token = tokenGenerator.Generate(user);
        return LoginResult.Succeeded(
            token.AccessToken,
            token.ExpiresIn,
            new LoginUser(user.Id, user.Name, user.Email, user.Role.ToString()));
    }
}

public sealed record LoginUser(Guid Id, string Name, string Email, string Role);

public sealed record LoginResult(
    bool IsSuccess,
    string? ErrorCode,
    string? AccessToken,
    int ExpiresIn,
    LoginUser? User)
{
    public static LoginResult Failed(string code) => new(false, code, null, 0, null);

    public static LoginResult Succeeded(string accessToken, int expiresIn, LoginUser user) =>
        new(true, null, accessToken, expiresIn, user);
}
