using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductRequests.Domain.Offers;
using ProductRequests.Domain.ProductRequests;
using ProductRequests.Domain.Users;

namespace ProductRequests.Infrastructure.Persistence.Configurations;

internal sealed class OfferHistoryConfiguration : IEntityTypeConfiguration<OfferHistory>
{
    public void Configure(EntityTypeBuilder<OfferHistory> builder)
    {
        builder.ToTable("OfferHistories");
        builder.HasAnnotation("MySQL:Engine", "InnoDB");
        builder.HasAnnotation("MySQL:Charset", "utf8mb4");
        builder.HasKey(history => history.Id);
        builder.Property(history => history.Id).ValueGeneratedNever();
        builder.Property(history => history.ActorRole).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(history => history.Action).HasConversion<string>().HasMaxLength(60).IsRequired();
        builder.Property(history => history.PreviousStatus).HasConversion<string>().HasMaxLength(40);
        builder.Property(history => history.NewStatus).HasConversion<string>().HasMaxLength(40);
        builder.Property(history => history.Amount).HasColumnType("decimal(18,2)");
        builder.Property(history => history.Comment).HasMaxLength(1000);
        builder.Property(history => history.OccurredAt)
            .HasConversion(DateTimeOffsetConverters.Utc)
            .HasColumnType("datetime(6)");
        builder.HasIndex(history => new { history.OfferId, history.OccurredAt })
            .HasDatabaseName("IX_OfferHistories_OfferId_OccurredAt");
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(history => history.ActorId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ProductRequest>()
            .WithMany()
            .HasForeignKey(history => history.ProductRequestId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
