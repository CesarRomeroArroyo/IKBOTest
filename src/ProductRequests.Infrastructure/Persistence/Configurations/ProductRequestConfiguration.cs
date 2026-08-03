using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductRequests.Domain.ProductRequests;
using ProductRequests.Domain.Users;

namespace ProductRequests.Infrastructure.Persistence.Configurations;

internal sealed class ProductRequestConfiguration : IEntityTypeConfiguration<ProductRequest>
{
    public void Configure(EntityTypeBuilder<ProductRequest> builder)
    {
        builder.ToTable("ProductRequests");
        builder.HasAnnotation("MySQL:Engine", "InnoDB");
        builder.HasAnnotation("MySQL:Charset", "utf8mb4");
        builder.HasKey(request => request.Id);
        builder.Property(request => request.Id).ValueGeneratedNever();
        builder.Property(request => request.ProductName).HasMaxLength(200).IsRequired();
        builder.Property(request => request.Description).HasMaxLength(4000).IsRequired();
        builder.Property(request => request.Currency).HasColumnType("char(3)").IsRequired();
        builder.Property(request => request.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(request => request.Version).IsConcurrencyToken();
        builder.Property(request => request.CreatedAt)
            .HasConversion(DateTimeOffsetConverters.Utc)
            .HasColumnType("datetime(6)");
        builder.Property(request => request.UpdatedAt)
            .HasConversion(DateTimeOffsetConverters.Utc)
            .HasColumnType("datetime(6)");
        builder.HasIndex(request => request.Status).HasDatabaseName("IX_ProductRequests_Status");
        builder.HasIndex(request => request.ClientId).HasDatabaseName("IX_ProductRequests_ClientId");
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(request => request.ClientId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(request => request.Offers)
            .WithOne()
            .HasForeignKey(offer => offer.ProductRequestId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(request => request.Offers).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
