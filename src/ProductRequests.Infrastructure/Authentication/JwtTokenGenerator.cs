using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ProductRequests.Application.Auth;
using ProductRequests.Domain.Users;

namespace ProductRequests.Infrastructure.Authentication;

internal sealed class JwtTokenGenerator(IOptions<JwtOptions> options) : ITokenGenerator
{
    private readonly JwtOptions _options = options.Value;

    public TokenResult Generate(User user)
    {
        if (_options.SigningKey.Length < 32 || string.IsNullOrWhiteSpace(_options.Issuer) ||
            string.IsNullOrWhiteSpace(_options.Audience))
        {
            throw new InvalidOperationException("Valid JWT issuer, audience and signing key are required.");
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        DateTimeOffset expires = now.AddMinutes(_options.ExpirationMinutes);
        Claim[] claims =
        [
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("role", user.Role.ToString())
        ];
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            _options.Issuer,
            _options.Audience,
            claims,
            now.UtcDateTime,
            expires.UtcDateTime,
            credentials);
        return new TokenResult(
            new JwtSecurityTokenHandler().WriteToken(token),
            checked((int)(expires - now).TotalSeconds));
    }
}
