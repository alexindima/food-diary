using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class BodyMetricsIdConversionTests {
    [Fact]
    public void WaistEntryId_PreservesValueAcrossConversionsAndFormatting() {
        var value = Guid.Parse("12345678-1234-1234-1234-1234567890ab");
        var id = (WaistEntryId)value;
        Guid roundTrip = id;

        Assert.Multiple(
            () => Assert.Equal(value, roundTrip),
            () => Assert.Equal(value, id.Value),
            () => Assert.Equal(value.ToString(), id.ToString()),
            () => Assert.Equal(Guid.Empty, WaistEntryId.Empty.Value));
    }
    [Fact]
    public void WeightEntryId_PreservesValueAcrossConversionsAndFormatting() {
        var value = Guid.Parse("12345678-1234-1234-1234-1234567890ab");
        var id = (WeightEntryId)value;
        Guid roundTrip = id;

        Assert.Multiple(
            () => Assert.Equal(value, roundTrip),
            () => Assert.Equal(value, id.Value),
            () => Assert.Equal(value.ToString(), id.ToString()),
            () => Assert.Equal(Guid.Empty, WeightEntryId.Empty.Value));
    }
}
