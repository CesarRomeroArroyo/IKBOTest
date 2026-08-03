using System.Data.Common;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using ProductRequests.Api.ExceptionHandling;
using ProductRequests.Domain.Common;
using ProductRequests.Domain.ProductRequests;
using ProductRequests.Domain.Users;
using ProductRequests.Infrastructure.Persistence;
using Offer = ProductRequests.Domain.Offers.Offer;

namespace ProductRequests.IntegrationTests;

[Collection(MySqlDatabaseFixtureSet.Name)]
public sealed class PersistenceTests(MySqlFixture fixture)
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void EfModelBuildsWithRequiredConcurrencyTokens()
    {
        using ProductRequestsDbContext context = fixture.CreateContext();

        Assert.True(context.Model.FindEntityType(typeof(ProductRequest))!
            .FindProperty(nameof(ProductRequest.Version))!.IsConcurrencyToken);
        Assert.True(context.Model.FindEntityType(typeof(Offer))!
            .FindProperty(nameof(Offer.Version))!.IsConcurrencyToken);
    }

    [Fact]
    public async Task MigrationUsesInnoDbUtf8Mb4AndCriticalIndexes()
    {
        await using ProductRequestsDbContext context = fixture.CreateContext();
        await context.Database.OpenConnectionAsync();
        await using DbCommand command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM information_schema.tables
            WHERE table_schema = DATABASE()
              AND table_name IN ('Users', 'ProductRequests', 'Offers', 'OfferHistories')
              AND engine = 'InnoDB'
              AND table_collation LIKE 'utf8mb4%';
            """;

        long tableCount = Convert.ToInt64(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture);

        Assert.Equal(4, tableCount);
        Assert.Contains(context.Model.GetEntityTypes().SelectMany(entity => entity.GetIndexes()),
            index => index.GetDatabaseName() == "UX_Offers_ProductRequestId_ProviderId" && index.IsUnique);
    }

    [Fact]
    public async Task UniqueNormalizedEmailIsEnforced()
    {
        await using ProductRequestsDbContext context = fixture.CreateContext();
        context.Users.Add(User.Create(Guid.NewGuid(), "First", "unique@example.com", "hash", UserRole.Client, Now));
        context.Users.Add(User.Create(Guid.NewGuid(), "Second", "UNIQUE@example.com", "hash", UserRole.Client, Now));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task UniqueProviderOfferIsEnforcedByMySql()
    {
        (Guid requestId, Guid providerId, Guid offerId) = await SeedRequestWithOfferAsync();
        await using ProductRequestsDbContext context = fixture.CreateContext();
        await context.Database.OpenConnectionAsync();
        await using DbCommand command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = """
            INSERT INTO Offers
              (Id, ProductRequestId, ProviderId, ProposedAmount, CounterAmount, AgreedAmount,
               DeliveryDays, Notes, Status, CreatedAt, UpdatedAt, Version)
            VALUES
              (@id, @requestId, @providerId, 50, NULL, NULL, 3, NULL,
               'PendingClientDecision', UTC_TIMESTAMP(6), UTC_TIMESTAMP(6), @version);
            """;
        AddParameter(command, "@id", Guid.NewGuid());
        AddParameter(command, "@requestId", requestId);
        AddParameter(command, "@providerId", providerId);
        AddParameter(command, "@version", Guid.NewGuid());

        Exception error = await Assert.ThrowsAnyAsync<Exception>(() => command.ExecuteNonQueryAsync());

        Assert.Contains("UX_Offers_ProductRequestId_ProviderId", error.Message, StringComparison.Ordinal);
        ExceptionDescriptor descriptor = ExceptionDescriptor.From(new DbUpdateException("duplicate", error));
        Assert.Equal("DUPLICATE_PROVIDER_OFFER", descriptor.Code);
        Assert.NotEqual(Guid.Empty, offerId);
    }

    [Fact]
    public async Task ConcurrentRootUpdatesProduceConflict()
    {
        Guid clientId = Guid.NewGuid();
        Guid provider1 = Guid.NewGuid();
        Guid provider2 = Guid.NewGuid();
        Guid requestId;
        await using (ProductRequestsDbContext setup = fixture.CreateContext())
        {
            setup.Users.AddRange(
                User.Create(clientId, "Client", $"{clientId}@example.com", "hash", UserRole.Client, Now),
                User.Create(provider1, "Provider 1", $"{provider1}@example.com", "hash", UserRole.Provider, Now),
                User.Create(provider2, "Provider 2", $"{provider2}@example.com", "hash", UserRole.Provider, Now));
            ProductRequest request = ProductRequest.Create(clientId, "Laptop", "Business", 1, "USD", Now);
            requestId = request.Id;
            setup.ProductRequests.Add(request);
            await setup.SaveChangesAsync();
        }

        await using ProductRequestsDbContext first = fixture.CreateContext();
        await using ProductRequestsDbContext second = fixture.CreateContext();
        ProductRequest firstCopy = await first.ProductRequests.Include(item => item.Offers).SingleAsync(item => item.Id == requestId);
        ProductRequest secondCopy = await second.ProductRequests.Include(item => item.Offers).SingleAsync(item => item.Id == requestId);
        Guid originalVersion = firstCopy.Version;
        first.Entry(firstCopy).Property(item => item.Version).CurrentValue = Guid.NewGuid();
        second.Entry(secondCopy).Property(item => item.Version).CurrentValue = Guid.NewGuid();

        Assert.NotEqual(originalVersion, firstCopy.Version);
        Assert.NotEqual(originalVersion, secondCopy.Version);
        Assert.Equal(originalVersion, first.Entry(firstCopy).Property(item => item.Version).OriginalValue);
        Assert.Equal(originalVersion, second.Entry(secondCopy).Property(item => item.Version).OriginalValue);

        await first.SaveChangesAsync();

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
    }

    [Fact]
    public async Task HistoryCannotBeDeletedThroughOfferCascade()
    {
        (_, _, Guid offerId) = await SeedRequestWithOfferAsync();
        await using ProductRequestsDbContext context = fixture.CreateContext();
        Offer offer = await context.Offers.SingleAsync(item => item.Id == offerId);
        context.Offers.Remove(offer);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    private async Task<(Guid RequestId, Guid ProviderId, Guid OfferId)> SeedRequestWithOfferAsync()
    {
        Guid clientId = Guid.NewGuid();
        Guid providerId = Guid.NewGuid();
        await using ProductRequestsDbContext context = fixture.CreateContext();
        context.Users.AddRange(
            User.Create(clientId, "Client", $"{clientId}@example.com", "hash", UserRole.Client, Now),
            User.Create(providerId, "Provider", $"{providerId}@example.com", "hash", UserRole.Provider, Now));
        ProductRequest request = ProductRequest.Create(clientId, "Laptop", "Business", 1, "USD", Now);
        Offer offer = request.AddOffer(providerId, new Money(100, "USD"), 5, null, Now);
        context.ProductRequests.Add(request);
        await context.SaveChangesAsync();
        return (request.Id, providerId, offer.Id);
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        DbParameter parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
