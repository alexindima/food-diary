using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Resources.Notifications;

namespace FoodDiary.Resources.Tests.Notifications;

public sealed class NotificationResourceRendererTests {
    [Fact]
    public void RenderFromPayload_WhenPersonalizedPayloadIsMalformed_ReturnsFallbackText() {
        var renderer = new NotificationResourceRenderer();

        var text = renderer.RenderFromPayload(NotificationTypes.NewRecommendation, "{", "en");

        Assert.Equal("New recommendation from your dietologist", text.Title);
    }

    [Fact]
    public void RenderFromPayload_WhenInvitationPayloadIsMalformed_UsesDefaultArgument() {
        var renderer = new NotificationResourceRenderer();

        var text = renderer.RenderFromPayload(NotificationTypes.DietologistInvitationReceived, "{", "en");

        Assert.Equal("New dietologist invitation", text.Title);
        Assert.Equal(
            "A client invited you to become their dietologist. Open the invitation to accept or decline.",
            text.Body);
    }

    [Fact]
    public void RenderFromPayload_WithRussianLocale_ReturnsReadableCyrillicText() {
        var renderer = new NotificationResourceRenderer();

        var text = renderer.RenderFromPayload(
            NotificationTypes.NewRecommendation,
            NotificationPayloads.NewRecommendation("Анна"),
            "ru");

        Assert.Equal("Новая рекомендация от вашего диетолога Анна", text.Title);
        Assert.DoesNotContain("Ð", text.Title);
        Assert.DoesNotContain("Ñ", text.Title);
        Assert.DoesNotContain("�", text.Title);
    }
}
