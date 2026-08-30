using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class WearableIdConversionTests {
    [Fact]
    public void WearableIds_PreserveGuidAcrossConversionsAndFormatting() {
        var value = Guid.Parse("12345678-1234-1234-1234-1234567890ab");
        var connection = (WearableConnectionId)value;
        var sync = (WearableSyncEntryId)value;

        Assert.Multiple(
            () => Assert.Equal(value, (Guid)connection),
            () => Assert.Equal(value.ToString(), connection.ToString()),
            () => Assert.Equal(Guid.Empty, WearableConnectionId.Empty.Value),
            () => Assert.Equal(value, (Guid)sync),
            () => Assert.Equal(value.ToString(), sync.ToString()),
            () => Assert.Equal(Guid.Empty, WearableSyncEntryId.Empty.Value));
    }
}
