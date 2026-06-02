using FoodDiary.Domain.Entities.OpenFoodFacts;

namespace FoodDiary.Application.Tests.Domain;

public sealed class OpenFoodFactsProductTests {
    [Fact]
    public void Create_WithValidValues_NormalizesFieldsAndMarksSeen() {
        var syncedAt = DateTime.UtcNow;

        var product = OpenFoodFactsProduct.Create(
            barcode: " 4600000000001 ",
            name: " Milk ",
            brand: " Brand ",
            category: " Dairy ",
            imageUrl: " https://example.com/image.jpg ",
            caloriesPer100G: 64,
            proteinsPer100G: 3.2,
            fatsPer100G: 3.5,
            carbsPer100G: 4.8,
            fiberPer100G: 0,
            syncedAtUtc: syncedAt);

        Assert.Equal("4600000000001", product.Barcode);
        Assert.Equal("Milk", product.Name);
        Assert.Equal("Brand", product.Brand);
        Assert.Equal("Dairy", product.Category);
        Assert.Equal("https://example.com/image.jpg", product.ImageUrl);
        Assert.Equal(64, product.CaloriesPer100G);
        Assert.Equal(3.2, product.ProteinsPer100G);
        Assert.Equal(3.5, product.FatsPer100G);
        Assert.Equal(4.8, product.CarbsPer100G);
        Assert.Equal(0, product.FiberPer100G);
        Assert.Equal(syncedAt, product.LastSyncedAtUtc);
        Assert.Equal(syncedAt, product.LastSeenAtUtc);
        Assert.Equal(1, product.SearchHitCount);
    }

    [Fact]
    public void Update_ReplacesValuesAndNormalizesOptionalWhitespaceToNull() {
        var product = OpenFoodFactsProduct.Create(
            "4600000000001",
            "Milk",
            "Brand",
            "Dairy",
            "https://example.com/image.jpg",
            64,
            3.2,
            3.5,
            4.8,
            0,
            DateTime.UtcNow.AddDays(-1));
        var syncedAt = DateTime.UtcNow;

        product.Update(
            name: " Kefir ",
            brand: " ",
            category: "",
            imageUrl: null,
            caloriesPer100G: 50,
            proteinsPer100G: 2.8,
            fatsPer100G: 2.5,
            carbsPer100G: 4,
            fiberPer100G: null,
            syncedAtUtc: syncedAt);

        Assert.Equal("Kefir", product.Name);
        Assert.Null(product.Brand);
        Assert.Null(product.Category);
        Assert.Null(product.ImageUrl);
        Assert.Equal(50, product.CaloriesPer100G);
        Assert.Equal(2.8, product.ProteinsPer100G);
        Assert.Equal(2.5, product.FatsPer100G);
        Assert.Equal(4, product.CarbsPer100G);
        Assert.Null(product.FiberPer100G);
        Assert.Equal(syncedAt, product.LastSyncedAtUtc);
        Assert.Equal(syncedAt, product.LastSeenAtUtc);
        Assert.Equal(2, product.SearchHitCount);
    }

    [Fact]
    public void MarkSeen_UpdatesLastSeenAndIncrementsHitCount() {
        var product = OpenFoodFactsProduct.Create(
            "4600000000001",
            "Milk",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            DateTime.UtcNow.AddDays(-1));
        var seenAt = DateTime.UtcNow;

        product.MarkSeen(seenAt);

        Assert.Equal(seenAt, product.LastSeenAtUtc);
        Assert.Equal(2, product.SearchHitCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WithMissingBarcode_Throws(string barcode) {
        Assert.Throws<ArgumentException>(() =>
            OpenFoodFactsProduct.Create(barcode, "Milk", null, null, null, null, null, null, null, null, DateTime.UtcNow));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WithMissingName_Throws(string name) {
        Assert.Throws<ArgumentException>(() =>
            OpenFoodFactsProduct.Create("4600000000001", name, null, null, null, null, null, null, null, null, DateTime.UtcNow));
    }
}
