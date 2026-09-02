
namespace FoodDiary.Domain.Primitives.Tests.ValueObjects;

[ExcludeFromCodeCoverage]
public sealed class ValueObjectsInvariantTestsEmailAddressTests {
    [Fact]
    public void EmailAddress_Create_NormalizesValue() {
        var email = EmailAddress.Create("  USER@Example.COM ");

        Assert.Equal("user@example.com", email.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid")]
    [InlineData("user@")]
    public void EmailAddress_Create_WithInvalidValue_Throws(string value) {
        Assert.Throws<ArgumentException>(() => EmailAddress.Create(value));
    }
}
