using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductRequests.Application.Auth;

namespace ProductRequests.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(AuthService authService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        LoginResult result = await authService.LoginAsync(request.Email, request.Password, cancellationToken);
        if (!result.IsSuccess)
        {
            return Unauthorized(new { code = result.ErrorCode });
        }

        return Ok(new LoginResponse(result.AccessToken!, result.ExpiresIn, result.User!));
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me() => Ok(new
    {
        id = User.FindFirst("sub")?.Value,
        email = User.FindFirst("email")?.Value,
        role = User.FindFirst("role")?.Value
    });
}

public sealed record LoginRequest(string? Email, string? Password);
public sealed record LoginResponse(string AccessToken, int ExpiresIn, LoginUser User);
