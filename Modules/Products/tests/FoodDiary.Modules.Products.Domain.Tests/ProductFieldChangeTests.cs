using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Modules.Products.Domain.Entities;
using FoodDiary.Modules.Products.Domain.ValueObjects;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Products.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class ProductFieldChangeTests {
    [Fact]
    public void SuppliedBlankText_ClearsExistingFieldsAndOmittedTextKeepsThem() {
        var product = Product.Create(UserId.New(), "Food", MeasurementUnit.G, 100, defaultPortionAmount: null,
            1, 1, 1, 1, 1, 0, barcode: "123", brand: "Brand", description: "Description");
        product.UpdateCoreIdentityChanges(new ProductCoreIdentityChanges(Name: null,
            FieldChanges.FromOptionalText(" ", clear: false), FieldChanges.Unchanged<string>(), ProductType: null));
        Assert.Null(product.Barcode);
        Assert.Equal("Brand", product.Brand);
        product.UpdateDescriptiveIdentityChanges(new ProductDescriptiveIdentityChanges(
            FieldChanges.Unchanged<string>(), FieldChanges.FromOptionalText("", clear: false), FieldChanges.Unchanged<string>()));
        Assert.Null(product.Description);
        DateTime? modified = product.ModifiedOnUtc;
        product.UpdateDescriptiveIdentityChanges(new ProductDescriptiveIdentityChanges(
            FieldChanges.Unchanged<string>(), FieldChanges.Clear<string>(), FieldChanges.Unchanged<string>()));
        Assert.Equal(modified, product.ModifiedOnUtc);
    }
}
