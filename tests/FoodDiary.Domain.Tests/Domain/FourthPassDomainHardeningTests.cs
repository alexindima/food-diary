using System.Reflection;
using FoodDiary.Domain.Entities.Achievements;
using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.Entities.OpenFoodFacts;
using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class FourthPassDomainHardeningTests {
    private static readonly DateTime Now = new(2026, 8, 19, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ReplaceRoles_DeduplicatesRolesByIdentifier() {
        var user = User.Create("roles@example.com", "hash");
        var admin = Role.Create("Admin");

        user.ReplaceRoles([admin, admin]);

        UserRole assignedRole = Assert.Single(user.UserRoles);
        Assert.Equal(admin.Id, assignedRole.RoleId);
    }

    [Fact]
    public void ClientTask_RejectsUnspecifiedDomainTimestamps() {
        Assert.Throws<ArgumentOutOfRangeException>(() => ClientTask.Create(
            UserId.New(),
            UserId.New(),
            "Task",
            details: null,
            new DateTime(2026, 8, 20)));

        var task = ClientTask.Create(
            UserId.New(),
            UserId.New(),
            "Task",
            details: null,
            Now.AddDays(1));

        Assert.Throws<ArgumentOutOfRangeException>(() => task.MarkDueReminderSent(new DateTime(2026, 8, 20)));
        Assert.Null(task.DueReminderSentAtUtc);
    }

    [Fact]
    public void RefreshTokenSession_RejectsTimestampsBeforeLastRotationAtomically() {
        var session = UserRefreshTokenSession.Create(
            Guid.NewGuid(),
            UserId.New(),
            "initial-hash",
            rememberMe: false,
            authProvider: null,
            ipAddress: null,
            userAgent: null,
            Now);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            session.Rotate("rewound-hash", rememberMe: true, Now.AddTicks(-1), TimeSpan.FromMinutes(5)));
        Assert.Equal("initial-hash", session.RefreshTokenHash);
        Assert.Equal(Now, session.LastRotatedAtUtc);

        session.Rotate("rotated-hash", rememberMe: true, Now.AddMinutes(1), TimeSpan.FromMinutes(5));
        Assert.Throws<ArgumentOutOfRangeException>(() => session.Revoke(Now));
        Assert.True(session.IsActive);
        Assert.Null(session.RevokedAtUtc);
    }

    [Fact]
    public void FastingOccurrence_CheckInRequiresActiveChronologicalOccurrence() {
        var occurrence = FastingOccurrence.Create(
            FastingPlanId.New(),
            UserId.New(),
            FastingOccurrenceKind.FastDay,
            Now,
            sequenceNumber: 1,
            targetHours: 24);

        Assert.Throws<ArgumentOutOfRangeException>(() => occurrence.UpdateCheckIn(
            1, 2, 3, symptoms: null, checkInNotes: null, Now.AddTicks(-1)));
        Assert.Null(occurrence.CheckInAtUtc);

        occurrence.Complete(Now.AddHours(24));
        Assert.Throws<InvalidOperationException>(() => occurrence.UpdateCheckIn(
            1, 2, 3, symptoms: null, checkInNotes: null, Now.AddHours(12)));
    }

    [Fact]
    public void BillingWebhookEvent_EnforcesMonotonicTerminalTransitions() {
        var webhookEvent = BillingWebhookEvent.CreateReceived(
            BillingProviderNames.Stripe,
            "evt_1",
            "invoice.paid",
            externalObjectId: null,
            Now,
            "{}",
            "{}");

        Assert.Throws<ArgumentOutOfRangeException>(() => webhookEvent.MarkProcessed(Now.AddTicks(-1)));
        Assert.Equal(BillingWebhookEvent.ReceivedStatus, webhookEvent.Status);

        webhookEvent.MarkFailed(Now.AddMinutes(1), "temporary");
        Assert.Throws<ArgumentOutOfRangeException>(() => webhookEvent.MarkProcessed(Now.AddSeconds(30)));
        Assert.Equal(BillingWebhookEvent.FailedStatus, webhookEvent.Status);

        webhookEvent.MarkProcessed(Now.AddMinutes(2));
        Assert.Throws<InvalidOperationException>(() => webhookEvent.MarkFailed(Now.AddMinutes(3), "late failure"));
        Assert.Equal(BillingWebhookEvent.ProcessedStatus, webhookEvent.Status);
    }

    [Fact]
    public void OpenFoodFactsSearchHitCount_SaturatesAtMaximum() {
        var product = OpenFoodFactsProduct.Create(
            "4600000000001",
            "Milk",
            brand: null,
            category: null,
            imageUrl: null,
            caloriesPer100G: null,
            proteinsPer100G: null,
            fatsPer100G: null,
            carbsPer100G: null,
            fiberPer100G: null,
            Now);
        SetProperty(product, nameof(OpenFoodFactsProduct.SearchHitCount), int.MaxValue);

        product.MarkSeen(Now.AddMinutes(1));

        Assert.Equal(int.MaxValue, product.SearchHitCount);
        Assert.Equal(Now.AddMinutes(1), product.LastSeenAtUtc);
    }

    [Fact]
    public void VersionedEntities_RejectOverflowBeforeMutation() {
        var template = AiPromptTemplate.Create("key", "en", "old text");
        SetProperty(template, nameof(AiPromptTemplate.Version), int.MaxValue);

        Assert.Throws<InvalidOperationException>(() => template.Update("new text"));
        Assert.Equal("old text", template.PromptText);
        Assert.Equal(int.MaxValue, template.Version);
        Assert.Null(template.ModifiedOnUtc);

        var definition = AchievementDefinition.Create(
            "meals_10",
            "habits",
            AchievementMetric.TotalMeals,
            threshold: 10,
            "Название",
            "Title",
            "Описание",
            "Description",
            "trophy",
            sortOrder: 1);
        SetProperty(definition, nameof(AchievementDefinition.Version), int.MaxValue);

        Assert.Throws<InvalidOperationException>(() => definition.Update(
            "nutrition",
            AchievementMetric.TotalMeals,
            threshold: 20,
            "Новое название",
            "New title",
            "Новое описание",
            "New description",
            "restaurant",
            sortOrder: 2,
            isActive: false));
        Assert.Equal("habits", definition.Category);
        Assert.Equal(10, definition.Threshold);
        Assert.Equal(int.MaxValue, definition.Version);
        Assert.Null(definition.ModifiedOnUtc);
    }

    [Fact]
    public void RequiredTextInputs_RejectNullWithArgumentExceptions() {
        Assert.Throws<ArgumentNullException>(() => UserAchievement.Create(
            UserId.New(),
            achievementKey: null!,
            Now,
            earnedValue: 1,
            definitionVersion: 1));
        Assert.Throws<ArgumentException>(() => WebPushSubscription.Create(
            UserId.New(),
            endpoint: null!,
            p256Dh: "p256",
            auth: "auth"));
    }

    private static void SetProperty<T>(T instance, string propertyName, object value) {
        PropertyInfo property = typeof(T).GetProperty(propertyName) ?? throw new InvalidOperationException($"Property {propertyName} was not found.");
        property.SetValue(instance, value);
    }
}
