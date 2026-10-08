using FoodDiary.Modules.Hydration.Domain.Entities.Tracking;
using FoodDiary.Modules.Hydration.Domain.ValueObjects;
using FoodDiary.Modules.Hydration.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FoodDiary.Modules.Hydration.Infrastructure.Tests;

[ExcludeFromCodeCoverage]
public sealed class HydrationAmountMappingTests {
    [Fact]
    public void ValidatedAmount_DoesNotBecomeANewPersistenceEntityOrColumn() {
        DbContextOptions<HydrationDbContext> options = new DbContextOptionsBuilder<HydrationDbContext>()
            .UseNpgsql("Host=127.0.0.1;Database=metadata_only;Username=unused;Password=unused")
            .Options;
        using var context = new HydrationDbContext(options);

        IModel model = context.GetService<IDesignTimeModel>().Model;
        IEntityType? entity = model.FindEntityType(typeof(HydrationEntry));
        Assert.NotNull(entity);
        IProperty? amount = entity.FindProperty(nameof(HydrationEntry.AmountMl));
        Assert.NotNull(amount);

        Assert.Multiple(() => {
            Assert.Equal(typeof(int), amount.ClrType);
            Assert.False(amount.IsNullable);
            Assert.Equal("integer", amount.GetColumnType());
            Assert.Null(model.FindEntityType(typeof(HydrationAmount)));
            Assert.Contains(entity.GetCheckConstraints(), constraint => string.Equals(constraint.Name, "CK_HydrationEntries_AmountMl", StringComparison.Ordinal) &&
                string.Equals(constraint.Sql, "\"AmountMl\" > 0 AND \"AmountMl\" <= 10000", StringComparison.Ordinal));
        });
    }
}
