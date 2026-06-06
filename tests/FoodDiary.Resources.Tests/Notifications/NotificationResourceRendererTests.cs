using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Resources.Notifications;

namespace FoodDiary.Resources.Tests.Notifications;

[ExcludeFromCodeCoverage]
public sealed class NotificationResourceRendererTests {
    [Fact]
    public void Render_WithEmptyType_ThrowsArgumentException() {
        var renderer = new NotificationResourceRenderer();

        ArgumentException ex = Assert.Throws<ArgumentException>(() => renderer.Render(""));

        Assert.Equal("type", ex.ParamName);
    }

    [Fact]
    public void Render_WhenTemplateHasNoBody_ReturnsNullBody() {
        var renderer = new NotificationResourceRenderer();

        NotificationText text = renderer.Render(NotificationTypes.NewRecommendation, " ", string.Empty);

        Assert.Equal("New recommendation from your dietologist", text.Title);
        Assert.Null(text.Body);
    }

    [Fact]
    public void Render_WithNullLocale_UsesEnglishResources() {
        var renderer = new NotificationResourceRenderer();

        NotificationText text = renderer.Render(NotificationTypes.NewRecommendation, null, string.Empty);

        Assert.Equal("New recommendation from your dietologist", text.Title);
        Assert.Null(text.Body);
    }

    [Fact]
    public void Render_WithEnglishLocale_UsesEnglishResources() {
        var renderer = new NotificationResourceRenderer();

        NotificationText text = renderer.Render(NotificationTypes.NewRecommendation, "en", string.Empty);

        Assert.Equal("New recommendation from your dietologist", text.Title);
        Assert.Null(text.Body);
    }

    [Fact]
    public void Render_WithRussianLocale_UsesRussianResources() {
        var renderer = new NotificationResourceRenderer();

        NotificationText text = renderer.Render(NotificationTypes.NewRecommendation, "ru", string.Empty);

        Assert.Equal("\u041d\u043e\u0432\u0430\u044f \u0440\u0435\u043a\u043e\u043c\u0435\u043d\u0434\u0430\u0446\u0438\u044f \u043e\u0442 \u0432\u0430\u0448\u0435\u0433\u043e \u0434\u0438\u0435\u0442\u043e\u043b\u043e\u0433\u0430", text.Title);
        Assert.Null(text.Body);
    }

    [Fact]
    public void Render_WithRussianRegionLocale_UsesRussianResources() {
        var renderer = new NotificationResourceRenderer();

        NotificationText text = renderer.Render(NotificationTypes.NewRecommendation, "ru-RU", string.Empty);

        Assert.Equal("\u041d\u043e\u0432\u0430\u044f \u0440\u0435\u043a\u043e\u043c\u0435\u043d\u0434\u0430\u0446\u0438\u044f \u043e\u0442 \u0432\u0430\u0448\u0435\u0433\u043e \u0434\u0438\u0435\u0442\u043e\u043b\u043e\u0433\u0430", text.Title);
        Assert.Null(text.Body);
    }

    [Fact]
    public void Render_WithUnknownLocale_FallsBackToEnglishResources() {
        var renderer = new NotificationResourceRenderer();

        NotificationText text = renderer.Render(NotificationTypes.NewRecommendation, "ka-GE", string.Empty);

        Assert.Equal("New recommendation from your dietologist", text.Title);
        Assert.Null(text.Body);
    }

    [Fact]
    public void RenderFromPayload_WithEmptyPayload_ThrowsArgumentException() {
        var renderer = new NotificationResourceRenderer();

        ArgumentException ex = Assert.Throws<ArgumentException>(() => renderer.RenderFromPayload(NotificationTypes.NewRecommendation, ""));

        Assert.Equal("payloadJson", ex.ParamName);
    }

    [Fact]
    public void RenderFromPayload_WithNonPersonalizedType_UsesGenericRenderer() {
        var renderer = new NotificationResourceRenderer();

        NotificationText text = renderer.RenderFromPayload(NotificationTypes.NewComment, "{}", "en");

        Assert.Equal("New comment on your recipe", text.Title);
        Assert.Null(text.Body);
    }

    [Fact]
    public void RenderFromPayload_WhenPersonalizedPayloadIsMalformed_ReturnsFallbackText() {
        var renderer = new NotificationResourceRenderer();

        NotificationText text = renderer.RenderFromPayload(NotificationTypes.NewRecommendation, "{", "en");

        Assert.Equal("New recommendation from your dietologist", text.Title);
    }

    [Fact]
    public void RenderFromPayload_WhenInvitationPayloadIsMalformed_UsesDefaultArgument() {
        var renderer = new NotificationResourceRenderer();

        NotificationText text = renderer.RenderFromPayload(NotificationTypes.DietologistInvitationReceived, "{", "en");

        Assert.Equal("New dietologist invitation", text.Title);
        Assert.Equal(
            "A client invited you to become their dietologist. Open the invitation to accept or decline.",
            text.Body);
    }

    [Fact]
    public void RenderFromPayload_WithRussianLocale_ReturnsReadableCyrillicText() {
        var renderer = new NotificationResourceRenderer();

        NotificationText text = renderer.RenderFromPayload(
            NotificationTypes.NewRecommendation,
            NotificationPayloads.NewRecommendation("\u0410\u043d\u043d\u0430"),
            "ru");

        Assert.Equal("\u041d\u043e\u0432\u0430\u044f \u0440\u0435\u043a\u043e\u043c\u0435\u043d\u0434\u0430\u0446\u0438\u044f \u043e\u0442 \u0432\u0430\u0448\u0435\u0433\u043e \u0434\u0438\u0435\u0442\u043e\u043b\u043e\u0433\u0430 \u0410\u043d\u043d\u0430", text.Title);
        Assert.DoesNotContain("\u00c3\u0090", text.Title, StringComparison.Ordinal);
        Assert.DoesNotContain("\u00c3\u2018", text.Title, StringComparison.Ordinal);
        Assert.DoesNotContain("\u00ef\u00bf\u00bd", text.Title, StringComparison.Ordinal);
    }
}
