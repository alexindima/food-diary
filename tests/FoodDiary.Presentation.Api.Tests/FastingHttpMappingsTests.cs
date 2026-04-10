using FoodDiary.Presentation.Api.Features.Fasting.Mappings;
using FoodDiary.Presentation.Api.Features.Fasting.Requests;
using FoodDiary.Application.Fasting.Models;

namespace FoodDiary.Presentation.Api.Tests;

public sealed class FastingHttpMappingsTests {
    [Fact]
    public void StartFastingRequest_ToCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        var request = new StartFastingHttpRequest("F16_8", "Intermittent", 16, null, null, null, null, "Feeling good");

        var command = request.ToCommand(userId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal("F16_8", command.Protocol);
        Assert.Equal("Intermittent", command.PlanType);
        Assert.Equal(16, command.PlannedDurationHours);
        Assert.Equal("Feeling good", command.Notes);
    }

    [Fact]
    public void StartFastingRequest_WithDefaults_MapsNullOptionals() {
        var userId = Guid.NewGuid();
        var request = new StartFastingHttpRequest("F18_6");

        var command = request.ToCommand(userId);

        Assert.Null(command.PlannedDurationHours);
        Assert.Null(command.Notes);
    }

    [Fact]
    public void GetFastingHistoryQuery_MapsDateRange() {
        var userId = Guid.NewGuid();
        var from = DateTime.UtcNow.AddDays(-30);
        var to = DateTime.UtcNow;
        var httpQuery = new GetFastingHistoryHttpQuery(from, to);

        var query = httpQuery.ToHistoryQuery(userId);

        Assert.Equal(userId, query.UserId);
        Assert.Equal(from, query.From);
        Assert.Equal(to, query.To);
    }

    [Fact]
    public void ExtendActiveFastingRequest_ToCommand_MapsFields() {
        var userId = Guid.NewGuid();
        var request = new ExtendActiveFastingHttpRequest(24);

        var command = request.ToExtendCommand(userId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(24, command.AdditionalHours);
    }

    [Fact]
    public void SkipCyclicFastDay_ToCommand_MapsUserId() {
        var userId = Guid.NewGuid();

        var command = userId.ToSkipCyclicFastDayCommand();

        Assert.Equal(userId, command.UserId);
    }

    [Fact]
    public void PostponeCyclicFastDay_ToCommand_MapsUserId() {
        var userId = Guid.NewGuid();

        var command = userId.ToPostponeCyclicFastDayCommand();

        Assert.Equal(userId, command.UserId);
    }

    [Fact]
    public void FastingSessionModel_ToHttpResponse_MapsStatus() {
        var model = new FastingSessionModel(
            Guid.NewGuid(),
            DateTime.UtcNow.AddHours(-8),
            DateTime.UtcNow,
            16,
            8,
            24,
            "F16_8",
            "Intermittent",
            "FastingWindow",
            null,
            null,
            null,
            null,
            true,
            "Interrupted",
            "Stopped early",
            DateTime.UtcNow.AddHours(-1),
            3,
            4,
            5,
            ["good"],
            "Hydration was fine");

        var response = model.ToHttpResponse();

        Assert.Equal("Interrupted", response.Status);
        Assert.True(response.IsCompleted);
        Assert.Equal(16, response.InitialPlannedDurationHours);
        Assert.Equal(8, response.AddedDurationHours);
        Assert.Equal(24, response.PlannedDurationHours);
        Assert.Equal("Intermittent", response.PlanType);
        Assert.Equal("FastingWindow", response.OccurrenceKind);
        Assert.Equal(3, response.HungerLevel);
        Assert.Equal(4, response.EnergyLevel);
        Assert.Equal(5, response.MoodLevel);
        Assert.Equal(["good"], response.Symptoms);
        Assert.Equal("Hydration was fine", response.CheckInNotes);
    }
}
