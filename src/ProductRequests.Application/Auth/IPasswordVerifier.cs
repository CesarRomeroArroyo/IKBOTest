using ProductRequests.Domain.Users;

namespace ProductRequests.Application.Auth;

public interface IPasswordVerifier
{
    bool Verify(User user, string password);
}
