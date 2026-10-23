using System.Globalization;
using FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Common;
using FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Models;

namespace FoodDiary.Modules.MealPlanning.Application.Tests.ShoppingLists;

[ExcludeFromCodeCoverage]
public sealed class ShoppingListRevisionTests {
    [Fact]
    public void EveryPersistedListItemAndSourceFieldContributesToRevision() {
        ShoppingListModel baseline = CreateModel();
        ShoppingListItemModel item = Assert.Single(baseline.Items);
        ShoppingListItemSourceModel source = Assert.Single(item.Sources);
        ShoppingListModel[] changedLists = [
            baseline with { Id = Guid.NewGuid() },
            baseline with { Name = "Other" },
            baseline with { CreatedAt = baseline.CreatedAt.AddTicks(10) },
            baseline with { Items = [] },
        ];
        ShoppingListItemModel[] changedItems = [
            item with { Id = Guid.NewGuid() },
            item with { ShoppingListId = Guid.NewGuid() },
            item with { ProductId = null },
            item with { Name = "Other item" },
            item with { Amount = Math.BitIncrement(item.Amount!.Value) },
            item with { Amount = null },
            item with { Unit = null },
            item with { Category = null },
            item with { Aisle = null },
            item with { Note = null },
            item with { IsChecked = false },
            item with { CheckedOnUtc = null },
            item with { CheckedOnUtc = item.CheckedOnUtc!.Value.AddTicks(10) },
            item with { SortOrder = 2 },
            item with { Sources = [] },
        ];
        ShoppingListItemSourceModel[] changedSources = [
            source with { Id = Guid.NewGuid() },
            source with { SourceType = "Other" },
            source with { MealPlanId = null },
            source with { MealPlanMealId = null },
            source with { RecipeId = null },
            source with { Label = "Other label" },
            source with { DayNumber = null },
            source with { MealType = null },
            source with { Amount = Math.BitIncrement(source.Amount) },
            source with { Unit = null },
        ];

        Assert.Matches("^[0-9a-f]{64}$", baseline.Revision);
        Assert.All(changedLists, changed => Assert.NotEqual(baseline.Revision, changed.Revision, StringComparer.Ordinal));
        Assert.All(changedItems, changed => Assert.NotEqual(baseline.Revision, (baseline with { Items = [changed] }).Revision, StringComparer.Ordinal));
        Assert.All(changedSources, changed => Assert.NotEqual(baseline.Revision,
            (baseline with { Items = [item with { Sources = [changed] }] }).Revision, StringComparer.Ordinal));
    }

    [Fact]
    public void RevisionIsIndependentOfInputCollectionOrderIncludingSourceTies() {
        ShoppingListModel baseline = CreateModel();
        ShoppingListItemModel first = Assert.Single(baseline.Items);
        ShoppingListItemSourceModel source = Assert.Single(first.Sources);
        ShoppingListItemSourceModel tiedSource = source with { Id = Guid.NewGuid(), Amount = 3 };
        first = first with { Sources = [source, tiedSource] };
        ShoppingListItemModel second = first with { Id = Guid.NewGuid(), Name = "Other", Sources = [] };
        ShoppingListModel ordered = baseline with { Items = [first, second] };
        ShoppingListModel reversed = baseline with { Items = [second, first with { Sources = [tiedSource, source] }] };

        Assert.Equal(ordered.Revision, reversed.Revision);
    }

    [Fact]
    public void RevisionUsesCultureIndependentExactNumbersAndDistinctNulls() {
        ShoppingListModel baseline = CreateModel();
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        try {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            string english = baseline.Revision;
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ru-RU");
            Assert.Equal(english, baseline.Revision);
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ar-SA");
            Assert.Equal(english, baseline.Revision);
        } finally {
            CultureInfo.CurrentCulture = originalCulture;
        }

        ShoppingListItemModel item = Assert.Single(baseline.Items);
        Assert.NotEqual((baseline with { Items = [item with { Note = null }] }).Revision,
            (baseline with { Items = [item with { Note = string.Empty }] }).Revision, StringComparer.Ordinal);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(9, 0)]
    [InlineData(10, 10)]
    [InlineData(-1, 0)]
    [InlineData(-9, 0)]
    [InlineData(-10, -10)]
    [InlineData(-11, -10)]
    public void TimestampPrecisionTruncatesRelativeToPostgresEpoch(long offset, long expectedOffset) {
        DateTime epoch = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        Assert.Equal(epoch.AddTicks(expectedOffset), ShoppingListRevision.NormalizeTimestamp(epoch.AddTicks(offset)));
    }

    [Fact]
    public void TimestampNormalizationPreservesNullAndInfinitySentinels() {
        Assert.Null(ShoppingListRevision.NormalizeTimestamp((DateTime?)null));
        Assert.Equal(DateTime.MinValue.Ticks, ShoppingListRevision.NormalizeTimestamp(DateTime.MinValue).Ticks);
        Assert.Equal(DateTime.MaxValue.Ticks, ShoppingListRevision.NormalizeTimestamp(DateTime.MaxValue).Ticks);
        Assert.Equal(DateTimeKind.Utc, ShoppingListRevision.NormalizeTimestamp(DateTime.MaxValue).Kind);
    }

    private static ShoppingListModel CreateModel() {
        var listId = Guid.NewGuid();
        DateTime timestamp = new(2026, 10, 22, 10, 30, 0, DateTimeKind.Utc);
        var source = new ShoppingListItemSourceModel(Guid.NewGuid(), "MealPlan", Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), "Breakfast", 1, "Breakfast", 1.23456789012345, "G");
        var item = new ShoppingListItemModel(Guid.NewGuid(), listId, Guid.NewGuid(), "Rice", 1.23456789012345,
            "G", "Grain", "A1", "Note", IsChecked: true, timestamp, 1, [source]);
        return new ShoppingListModel(listId, "Weekly", timestamp, [item]);
    }
}
