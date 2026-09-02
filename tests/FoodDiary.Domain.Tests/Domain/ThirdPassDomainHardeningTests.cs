using System.Reflection;
using FoodDiary.Domain.Common;
using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.Entities.FavoriteMeals;
using FoodDiary.Domain.Entities.FavoriteProducts;
using FoodDiary.Domain.Entities.FavoriteRecipes;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class ThirdPassDomainHardeningTests {
    private static readonly DateTime Now = new(2026, 8, 19, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void RefreshTokenRotation_WhenValidationFails_IsAtomic() {
        var session = UserRefreshTokenSession.Create(
            Guid.NewGuid(),
            UserId.New(),
            "original-hash",
            rememberMe: true,
            authProvider: null,
            ipAddress: null,
            userAgent: null,
            Now);

        Assert.Throws<ArgumentException>(() =>
            session.Rotate(" ", rememberMe: false, Now.AddMinutes(1), TimeSpan.FromMinutes(5)));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            session.Rotate("next-hash", rememberMe: false, DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc), TimeSpan.FromTicks(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => UserRefreshTokenSession.Create(
            Guid.NewGuid(),
            UserId.New(),
            new string('h', 513),
            rememberMe: false,
            authProvider: null,
            ipAddress: null,
            userAgent: null,
            Now));

        Assert.Multiple(
            () => Assert.Equal("original-hash", session.RefreshTokenHash),
            () => Assert.True(session.RememberMe),
            () => Assert.Equal(Now, session.LastRotatedAtUtc),
            () => Assert.Null(session.PreviousRefreshTokenHash),
            () => Assert.Null(session.PreviousRefreshTokenValidUntilUtc),
            () => Assert.Null(session.ModifiedOnUtc));
    }

    [Fact]
    public void LinkAndUserFacingValues_RejectInvalidInput() {
        Assert.Throws<ArgumentException>(() => DietologistInvitation.Create(
            UserId.New(), "not-an-email", "hash", Now.AddDays(1), DietologistPermissions.AllEnabled));

        string longName = new('n', DomainConstants.CommentMaxLength + 1);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FavoriteProduct.Create(UserId.New(), ProductId.New(), longName));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FavoriteMeal.Create(UserId.New(), MealId.New(), longName));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FavoriteRecipe.Create(UserId.New(), RecipeId.New(), longName));
    }

    [Fact]
    public void BoundaryTransitions_HandleOverflowWithoutPartialMutation() {
        var user = User.Create("trial-overflow@example.com", "hash");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            user.StartPremiumTrial(DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc), TimeSpan.FromTicks(1)));
        Assert.Multiple(
            () => Assert.Null(user.PremiumTrialStartedAtUtc),
            () => Assert.Null(user.PremiumTrialEndsAtUtc));

        BillingWebhookEvent webhookEvent = CreateReceivedWebhookEvent();
        var boundary = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);
        webhookEvent.MarkFailed(boundary, "failure");
        Assert.Multiple(
            () => Assert.Equal(BillingWebhookEvent.FailedStatus, webhookEvent.Status),
            () => Assert.Equal(1, webhookEvent.AttemptCount),
            () => Assert.Equal("failure", webhookEvent.ErrorMessage),
            () => Assert.Equal(boundary, webhookEvent.NextAttemptAtUtc),
            () => Assert.Equal(boundary, webhookEvent.ModifiedOnUtc));

        BillingWebhookEvent saturatedEvent = CreateReceivedWebhookEvent();
        PropertyInfo attemptCountProperty = typeof(BillingWebhookEvent)
            .GetProperty(nameof(BillingWebhookEvent.AttemptCount))!;
        attemptCountProperty.SetValue(saturatedEvent, int.MaxValue);

        saturatedEvent.MarkFailed(Now.AddMinutes(1), "failure");

        Assert.Equal(int.MaxValue, saturatedEvent.AttemptCount);
    }

    private static BillingWebhookEvent CreateReceivedWebhookEvent() => BillingWebhookEvent.CreateReceived(
        BillingProviderNames.Stripe,
        Guid.NewGuid().ToString("N"),
        "payment.failed",
        externalObjectId: null,
        Now,
        "{}",
        "{}");
}
