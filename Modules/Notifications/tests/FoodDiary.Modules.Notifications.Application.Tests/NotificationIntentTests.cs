using FoodDiary.Modules.Notifications.Contracts.Common;
using FoodDiary.Modules.Notifications.Application.Abstractions.Common;

namespace FoodDiary.Modules.Notifications.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class NotificationIntentTests {
    [Fact]
    public void RecommendationCommentTarget_RoundTripsCompositeAndRetainsGuidFormatting() {
        const string client = "{02D9A7E0-F873-4775-8D84-D5B92F40EF07}";
        const string recommendation = "9796B9AFC6654F9B87F0CFEC3DD97647";
        var target = RecommendationCommentTarget.ForDietologist(recommendation, client);
        var intent = NotificationIntent.RecommendationComment(target);
        Assert.Equal($"{client}|{recommendation}", intent.ReferenceId);
        Assert.Equal(target, RecommendationCommentTarget.ParseDietologistReference(intent.ReferenceId!));
        Assert.Equal($"/dietologist/clients/{client}?recommendationId={recommendation}", NotificationTargetUrlResolver.Resolve(intent.Type, intent.ReferenceId));
    }

    [Theory]
    [InlineData("client|recommendation")]
    [InlineData("02d9a7e0-f873-4775-8d84-d5b92f40ef07")]
    [InlineData("02d9a7e0-f873-4775-8d84-d5b92f40ef07|9796b9af-c665-4f9b-87f0-cfec3dd97647|extra")]
    public void CompositeCodec_RetainsMalformedReferenceFallback(string reference) =>
        Assert.Null(RecommendationCommentTarget.ParseDietologistReference(reference));

    [Fact]
    public void Intent_CouplesPayloadWithTypeAndRetainsUnknownLegacyJson() {
        var known = NotificationIntent.NewRecommendation(new NewRecommendationNotificationPayload("Анна"));
        Assert.Equal(NotificationTypes.NewRecommendation, known.Type);
        Assert.Equal(NotificationPayloads.NewRecommendation("Анна"), known.PayloadJson);
        var legacy = NotificationIntent.FromLegacy("FutureType", "{ \"v\": 3 }", "raw-reference");
        Assert.True(legacy.IsLegacy);
        Assert.Equal("{ \"v\": 3 }", legacy.PayloadJson);
        Assert.Null(NotificationTargetUrlResolver.Resolve(legacy.Type, legacy.ReferenceId));
    }
}
