using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductRequests.Domain.Offers;
using ProductRequests.Domain.Users;

namespace ProductRequests.Infrastructure.Persistence.Configurations;

internal sealed class OfferConfiguration : IEntityTypeConfiguration<Offer>
{
    public void Configure(EntityTypeBuilder<Offer> builder)
    {
        builder.ToTable("Offers");
        builder.HasAnnotation("MySQL:Engine", "InnoDB");
        builder.HasAnnotation("MySQL:Charset", "utf8mb4");
        builder.HasKey(offer => offer.Id);
        builder.Property(offer => offer.Id).ValueGeneratedNever();
        builder.Property(offer => offer.ProposedAmount).HasColumnType("decimal(18,2)");
        builder.Property(offer => offer.CounterAmount).HasColumnType("decimal(18,2)");
        builder.Property(offer => offer.AgreedAmount).HasColumnType("decimal(18,2)");
        builder.Property(offer => offer.Notes).HasMaxLength(1000);
        builder.Property(offer => offer.Status).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(offer => offer.Version).IsConcurrencyToken();
        builder.Property(offer => offer.CreatedAt)
            .HasConversion(DateTimeOffsetConverters.Utc)
            .HasColumnType("datetime(6)");
        builder.Property(offer => offer.UpdatedAt)
            .HasConversion(DateTimeOffsetConverters.Utc)
            .HasColumnType("datetime(6)");
        builder.HasIndex(offer => offer.ProductRequestId).HasDatabaseName("IX_Offers_ProductRequestId");
        builder.HasIndex(offer => offer.ProviderId).HasDatabaseName("IX_Offers_ProviderId");
        builder.HasIndex(offer => new { offer.ProductRequestId, offer.ProviderId })
            .IsUnique()
            .HasDatabaseName("UX_Offers_ProductRequestId_ProviderId");
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(offer => offer.ProviderId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(offer => offer.Histories)
            .WithOne()
            .HasForeignKey(history => history.OfferId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(offer => offer.Histories).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
