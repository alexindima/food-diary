
namespace FoodDiary.Domain.Primitives.Tests.ValueObjects;

[ExcludeFromCodeCoverage]
public sealed class AdditionalValueObjectsInvariantTestsEmailAddressTests {
    [Fact]
    public void EmailAddress_Create_NormalizesAndToStringReturnsValue() {
        var email = EmailAddress.Create("  USER@Example.COM  ");

        Assert.Equal("user@example.com", email.Value);
        Assert.Equal(email.Value, email.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("not-an-email")]
    [InlineData("a@b@c")]
    public void EmailAddress_Create_WithInvalidValue_Throws(string value) {
        Assert.Throws<ArgumentException>(() => EmailAddress.Create(value));
    }

    [Fact]
    public void EmailAddress_Create_WithDisplayName_Throws() {
        Assert.Throws<ArgumentException>(() => EmailAddress.Create("User <user@example.com>"));
    }
}
