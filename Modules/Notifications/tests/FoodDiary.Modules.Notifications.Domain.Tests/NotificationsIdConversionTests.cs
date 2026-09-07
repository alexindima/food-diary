using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class NotificationsIdConversionTests {
    [Fact]
    public void NotificationId_PreservesValueAcrossConversionsAndFormatting() {
        var value = Guid.Parse("12345678-1234-1234-1234-1234567890ab");
        var id = (NotificationId)value;
        Guid roundTrip = id;

        Assert.Multiple(
            () => Assert.Equal(value, roundTrip),
            () => Assert.Equal(value, id.Value),
            () => Assert.Equal(value.ToString(), id.ToString()),
            () => Assert.Equal(Guid.Empty, NotificationId.Empty.Value));
    }
    [Fact]
    public void WebPushSubscriptionId_PreservesValueAcrossConversionsAndFormatting() {
        var value = Guid.Parse("12345678-1234-1234-1234-1234567890ab");
        var id = (WebPushSubscriptionId)value;
        Guid roundTrip = id;

        Assert.Multiple(
            () => Assert.Equal(value, roundTrip),
            () => Assert.Equal(value, id.Value),
            () => Assert.Equal(Guid.Empty, WebPushSubscriptionId.Empty.Value));
    }
}
