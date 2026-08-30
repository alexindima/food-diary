using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class MarketingIdConversionTests {
    [Fact]
    public void MarketingAttributionEventId_PreservesGuidAcrossConversionsAndFormatting() {
        var value = Guid.Parse("12345678-1234-1234-1234-1234567890ab");
        var id = (MarketingAttributionEventId)value;

        Assert.Multiple(
            () => Assert.Equal(value, (Guid)id),
            () => Assert.Equal(value.ToString(), id.ToString()),
            () => Assert.Equal(Guid.Empty, MarketingAttributionEventId.Empty.Value));
    }
}
