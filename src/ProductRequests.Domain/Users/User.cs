using ProductRequests.Domain.Exceptions;

namespace ProductRequests.Domain.Users;

public sealed class User
{
    private User()
    {
    }

    private User(Guid id, string name, string email, string passwordHash, UserRole role, DateTimeOffset createdAt)
    {
        Id = id;
        Name = name;
        Email = email;
        NormalizedEmail = NormalizeEmail(email);
        PasswordHash = passwordHash;
        Role = role;
        IsActive = true;
        CreatedAt = createdAt.ToUniversalTime();
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string NormalizedEmail { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static User Create(
        Guid id,
        string name,
        string email,
        string passwordHash,
        UserRole role,
        DateTimeOffset createdAt)
    {
        if (id == Guid.Empty || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException("VALIDATION_ERROR", "User data is required.");
        }

        return new User(id, name.Trim(), email.Trim(), passwordHash, role, createdAt);
    }

    public static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();

    public void Deactivate() => IsActive = false;
}
