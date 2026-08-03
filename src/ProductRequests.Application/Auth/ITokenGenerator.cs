using ProductRequests.Domain.Users;

namespace ProductRequests.Application.Auth;

public interface ITokenGenerator
{
    TokenResult Generate(User user);
}

public sealed record TokenResult(string AccessToken, int ExpiresIn);
