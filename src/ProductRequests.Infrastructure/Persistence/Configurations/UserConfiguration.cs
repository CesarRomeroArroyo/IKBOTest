using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductRequests.Domain.Users;

namespace ProductRequests.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasAnnotation("MySQL:Engine", "InnoDB");
        builder.HasAnnotation("MySQL:Charset", "utf8mb4");
        builder.HasKey(user => user.Id);
        builder.Property(user => user.Id).ValueGeneratedNever();
        builder.Property(user => user.Name).HasMaxLength(200).IsRequired();
        builder.Property(user => user.Email).HasMaxLength(320).IsRequired();
        builder.Property(user => user.NormalizedEmail).HasMaxLength(320).IsRequired();
        builder.Property(user => user.PasswordHash).HasMaxLength(1000).IsRequired();
        builder.Property(user => user.Role).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(user => user.CreatedAt)
            .HasConversion(DateTimeOffsetConverters.Utc)
            .HasColumnType("datetime(6)");
        builder.HasIndex(user => user.NormalizedEmail)
            .IsUnique()
            .HasDatabaseName("UX_Users_NormalizedEmail");
    }
}
