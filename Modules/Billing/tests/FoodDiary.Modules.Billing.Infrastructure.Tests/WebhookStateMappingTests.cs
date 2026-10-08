using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Modules.Billing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Billing.Infrastructure.Tests;

[ExcludeFromCodeCoverage]
public sealed class WebhookStateMappingTests {
    [Fact]
    public void TypedProcessingView_DoesNotChangeThePersistedStatusOrIndexes() {
        DbContextOptions<BillingDbContext> options = new DbContextOptionsBuilder<BillingDbContext>()
            .UseNpgsql("Host=localhost;Database=metadata_only;Username=unused")
            .Options;
        using var context = new BillingDbContext(options);
        Microsoft.EntityFrameworkCore.Metadata.IEntityType? entity = context.Model.FindEntityType(typeof(BillingWebhookEvent));
        Assert.NotNull(entity);
        Microsoft.EntityFrameworkCore.Metadata.IProperty? status = entity.FindProperty(nameof(BillingWebhookEvent.Status));
        Assert.NotNull(status);

        Assert.Multiple(() => {
            Assert.Equal("BillingWebhookEvents", entity.GetTableName());
            Assert.Equal(typeof(string), status.ClrType);
            Assert.False(status.IsNullable);
            Assert.Equal(32, status.GetMaxLength());
            Assert.Null(entity.FindProperty(nameof(BillingWebhookEvent.ProcessingState)));
            Assert.Contains(entity.GetIndexes(), index => index.Properties.Select(property => property.Name).SequenceEqual(
                [nameof(BillingWebhookEvent.Status), nameof(BillingWebhookEvent.NextAttemptAtUtc), nameof(BillingWebhookEvent.ReceivedAtUtc)], StringComparer.Ordinal));
            Assert.Contains(entity.GetIndexes(), index => index.IsUnique && index.Properties.Select(property => property.Name).SequenceEqual(
                [nameof(BillingWebhookEvent.Provider), nameof(BillingWebhookEvent.EventId)], StringComparer.Ordinal));
        });
    }
}
