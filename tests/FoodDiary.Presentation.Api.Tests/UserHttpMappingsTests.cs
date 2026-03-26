using FoodDiary.Presentation.Api.Features.Users.Mappings;
using FoodDiary.Presentation.Api.Features.Users.Models;
using FoodDiary.Presentation.Api.Features.Users.Requests;

namespace FoodDiary.Presentation.Api.Tests;

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
}
