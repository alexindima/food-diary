using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Application.Notifications.Models;
using FoodDiary.Application.Users.Models;
using FoodDiary.Presentation.Api.Features.Users.Mappings;
using FoodDiary.Presentation.Api.Features.Users.Models;
using FoodDiary.Presentation.Api.Features.Users.Requests;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class UserHttpMappingsTests {
    [Fact]
    public void UpdateUserRequest_ToCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        var profileImageAssetId = Guid.NewGuid();
        var birthDate = new DateTime(1995, 5, 20, 0, 0, 0, DateTimeKind.Utc);
        var request = new UpdateUserHttpRequest(
            Username: "alex",
            FirstName: "Alex",
            LastName: "Doe",
            BirthDate: birthDate,
            Gender: "Male",
            Weight: 82.4,
            Height: 181,
            ActivityLevel: "Moderate",
            StepGoal: 9000,
            HydrationGoal: 2.7,
            Language: "en",
            Theme: "leaf",
            UiStyle: "modern",
            PushNotificationsEnabled: true,
            FastingPushNotificationsEnabled: false,
            SocialPushNotificationsEnabled: true,
            ProfileImage: "https://cdn.example/profile.png",
            ProfileImageAssetId: profileImageAssetId,
            DashboardLayout: new DashboardLayoutHttpModel(
                Web: ["calories", "weight"],
                Mobile: ["hydration", "steps"]),
            IsActive: true);

        var command = request.ToCommand(userId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(request.Username, command.Username);
        Assert.Equal(request.FirstName, command.FirstName);
        Assert.Equal(request.LastName, command.LastName);
        Assert.Equal(request.BirthDate, command.BirthDate);
        Assert.Equal(request.Gender, command.Gender);
        Assert.Equal(request.Weight, command.Weight);
        Assert.Equal(request.Height, command.Height);
        Assert.Equal(request.ActivityLevel, command.ActivityLevel);
        Assert.Equal(request.StepGoal, command.StepGoal);
        Assert.Equal(request.HydrationGoal, command.HydrationGoal);
        Assert.Equal(request.Language, command.Language);
        Assert.Equal(request.Theme, command.Theme);
        Assert.Equal(request.UiStyle, command.UiStyle);
        Assert.Equal(request.PushNotificationsEnabled, command.PushNotificationsEnabled);
        Assert.Equal(request.FastingPushNotificationsEnabled, command.FastingPushNotificationsEnabled);
        Assert.Equal(request.SocialPushNotificationsEnabled, command.SocialPushNotificationsEnabled);
        Assert.Equal(request.ProfileImage, command.ProfileImage);
        Assert.Equal(request.ProfileImageAssetId, command.ProfileImageAssetId);
        Assert.NotNull(command.DashboardLayout);
        Assert.Equal(request.DashboardLayout!.Web, command.DashboardLayout!.Web);
        Assert.Equal(request.DashboardLayout.Mobile, command.DashboardLayout.Mobile);
        Assert.Equal(request.IsActive, command.IsActive);
    }

    [Fact]
    public void ChangePasswordRequest_ToCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        var request = new ChangePasswordHttpRequest(
            CurrentPassword: "old-password",
            NewPassword: "new-password");

        var command = request.ToCommand(userId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(request.CurrentPassword, command.CurrentPassword);
        Assert.Equal(request.NewPassword, command.NewPassword);
    }

    [Fact]
    public void UserIds_ToQueriesAndCommands_MapUserId() {
        var userId = Guid.NewGuid();

        Assert.Equal(userId, userId.ToUserQuery().UserId);
        Assert.Equal(userId, userId.ToProfileOverviewQuery().UserId);
        Assert.Equal(userId, userId.ToDesiredWeightQuery().UserId);
        Assert.Equal(userId, userId.ToDesiredWaistQuery().UserId);
        Assert.Equal(userId, userId.ToDeleteCommand().UserId);
        Assert.Equal(userId, userId.ToAcceptAiConsentCommand().UserId);
        Assert.Equal(userId, userId.ToRevokeAiConsentCommand().UserId);
    }

    [Fact]
    public void DesiredWeightAndWaistRequests_ToCommands_MapAllFields() {
        var userId = Guid.NewGuid();
        var weightRequest = new UpdateDesiredWeightHttpRequest(76.5);
        var waistRequest = new UpdateDesiredWaistHttpRequest(82.4);

        var weightCommand = weightRequest.ToDesiredWeightCommand(userId);
        var waistCommand = waistRequest.ToDesiredWaistCommand(userId);

        Assert.Equal(userId, weightCommand.UserId);
        Assert.Equal(76.5, weightCommand.DesiredWeight);
        Assert.Equal(userId, waistCommand.UserId);
        Assert.Equal(82.4, waistCommand.DesiredWaist);
    }

    [Fact]
    public void AppearanceAndSetPasswordRequests_ToCommands_MapAllFields() {
        var userId = Guid.NewGuid();
        var appearance = new UpdateUserAppearanceHttpRequest("dark", "compact");
        var password = new SetPasswordHttpRequest("new-password");

        var appearanceCommand = appearance.ToCommand(userId);
        var passwordCommand = password.ToCommand(userId);

        Assert.Equal(userId, appearanceCommand.UserId);
        Assert.Equal("dark", appearanceCommand.Theme);
        Assert.Equal("compact", appearanceCommand.UiStyle);
        Assert.Equal(userId, passwordCommand.UserId);
        Assert.Equal("new-password", passwordCommand.NewPassword);
    }

    [Fact]
    public void UserModel_ToHttpResponse_MapsAllFields() {
        var id = Guid.NewGuid();
        var profileImageAssetId = Guid.NewGuid();
        var birthDate = new DateTime(1995, 5, 20, 0, 0, 0, DateTimeKind.Utc);
        var lastLoginAtUtc = new DateTime(2026, 4, 6, 12, 0, 0, DateTimeKind.Utc);
        var aiConsentAcceptedAt = new DateTime(2026, 4, 5, 12, 0, 0, DateTimeKind.Utc);
        var model = CreateUserModel(
            id,
            profileImageAssetId,
            birthDate,
            lastLoginAtUtc,
            aiConsentAcceptedAt);

        var response = model.ToHttpResponse();

        Assert.Equal(id, response.Id);
        Assert.Equal("alex@example.com", response.Email);
        Assert.True(response.HasPassword);
        Assert.Equal("alex", response.Username);
        Assert.Equal("Alex", response.FirstName);
        Assert.Equal("Doe", response.LastName);
        Assert.Equal(birthDate, response.BirthDate);
        Assert.Equal("Male", response.Gender);
        Assert.Equal(82.4, response.Weight);
        Assert.Equal(78, response.DesiredWeight);
        Assert.Equal(84, response.DesiredWaist);
        Assert.Equal(181, response.Height);
        Assert.Equal("Moderate", response.ActivityLevel);
        Assert.Equal(2100, response.DailyCalorieTarget);
        Assert.Equal(120, response.ProteinTarget);
        Assert.Equal(70, response.FatTarget);
        Assert.Equal(220, response.CarbTarget);
        Assert.Equal(30, response.FiberTarget);
        Assert.Equal(9000, response.StepGoal);
        Assert.Equal(2.7, response.WaterGoal);
        Assert.Equal(2700, response.HydrationGoal);
        Assert.Equal("en", response.Language);
        Assert.Equal("leaf", response.Theme);
        Assert.Equal("modern", response.UiStyle);
        Assert.True(response.PushNotificationsEnabled);
        Assert.False(response.FastingPushNotificationsEnabled);
        Assert.True(response.SocialPushNotificationsEnabled);
        Assert.Equal(8, response.FastingCheckInReminderHours);
        Assert.Equal(2, response.FastingCheckInFollowUpReminderHours);
        Assert.Equal("https://cdn.example/profile.png", response.ProfileImage);
        Assert.Equal(profileImageAssetId, response.ProfileImageAssetId);
        Assert.Equal(["calories", "weight"], response.DashboardLayout!.Web);
        Assert.True(response.IsActive);
        Assert.True(response.IsEmailConfirmed);
        Assert.Equal(lastLoginAtUtc, response.LastLoginAtUtc);
        Assert.Equal(aiConsentAcceptedAt, response.AiConsentAcceptedAt);
    }

    [Fact]
    public void ProfileOverviewModel_ToHttpResponse_MapsNestedModels() {
        var userId = Guid.NewGuid();
        var subscriptionCreatedAtUtc = new DateTime(2026, 4, 6, 12, 0, 0, DateTimeKind.Utc);
        var relationshipId = Guid.NewGuid();
        var dietologistUserId = Guid.NewGuid();
        var model = new ProfileOverviewModel(
            CreateUserModel(userId, null, null, null, null),
            new NotificationPreferencesModel(
                PushNotificationsEnabled: true,
                FastingPushNotificationsEnabled: false,
                SocialPushNotificationsEnabled: true,
                FastingCheckInReminderHours: 6,
                FastingCheckInFollowUpReminderHours: 3),
            [
                new WebPushSubscriptionModel(
                    Endpoint: "https://push.example/subscription",
                    EndpointHost: "push.example",
                    ExpirationTimeUtc: null,
                    Locale: "ru",
                    UserAgent: "Firefox",
                    CreatedAtUtc: subscriptionCreatedAtUtc,
                    UpdatedAtUtc: subscriptionCreatedAtUtc.AddHours(1))
            ],
            new DietologistRelationshipModel(
                relationshipId,
                Status: "Accepted",
                Email: "dietologist@example.com",
                FirstName: "Dana",
                LastName: "Doc",
                dietologistUserId,
                new DietologistPermissionsModel(
                    ShareMeals: true,
                    ShareStatistics: true,
                    ShareWeight: false,
                    ShareWaist: true,
                    ShareGoals: false,
                    ShareHydration: true,
                    ShareProfile: false,
                    ShareFasting: true),
                CreatedAtUtc: subscriptionCreatedAtUtc.AddDays(-5),
                ExpiresAtUtc: subscriptionCreatedAtUtc.AddDays(5),
                AcceptedAtUtc: subscriptionCreatedAtUtc));

        var response = model.ToHttpResponse();

        Assert.Equal(userId, response.User.Id);
        Assert.True(response.NotificationPreferences.PushNotificationsEnabled);
        Assert.Single(response.WebPushSubscriptions);
        Assert.Equal("push.example", response.WebPushSubscriptions[0].EndpointHost);
        Assert.Equal(relationshipId, response.DietologistRelationship!.InvitationId);
        Assert.Equal(dietologistUserId, response.DietologistRelationship.DietologistUserId);
        Assert.True(response.DietologistRelationship.Permissions.ShareMeals);
    }

    private static UserModel CreateUserModel(
        Guid id,
        Guid? profileImageAssetId,
        DateTime? birthDate,
        DateTime? lastLoginAtUtc,
        DateTime? aiConsentAcceptedAt) =>
        new(
            id,
            "alex@example.com",
            HasPassword: true,
            Username: "alex",
            FirstName: "Alex",
            LastName: "Doe",
            BirthDate: birthDate,
            Gender: "Male",
            Weight: 82.4,
            DesiredWeight: 78,
            DesiredWaist: 84,
            Height: 181,
            ActivityLevel: "Moderate",
            DailyCalorieTarget: 2100,
            ProteinTarget: 120,
            FatTarget: 70,
            CarbTarget: 220,
            FiberTarget: 30,
            StepGoal: 9000,
            WaterGoal: 2.7,
            HydrationGoal: 2700,
            Language: "en",
            Theme: "leaf",
            UiStyle: "modern",
            PushNotificationsEnabled: true,
            FastingPushNotificationsEnabled: false,
            SocialPushNotificationsEnabled: true,
            FastingCheckInReminderHours: 8,
            FastingCheckInFollowUpReminderHours: 2,
            ProfileImage: "https://cdn.example/profile.png",
            ProfileImageAssetId: profileImageAssetId,
            DashboardLayout: new DashboardLayoutModel(["calories", "weight"], ["hydration"]),
            IsActive: true,
            IsEmailConfirmed: true,
            LastLoginAtUtc: lastLoginAtUtc,
            AiConsentAcceptedAt: aiConsentAcceptedAt);
}
