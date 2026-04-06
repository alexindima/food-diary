using FoodDiary.Presentation.Api.Features.Fasting.Mappings;
using FoodDiary.Presentation.Api.Features.Fasting.Requests;

namespace FoodDiary.Presentation.Api.Tests;

public sealed class FastingHttpMappingsTests {
    [Fact]
    public void StartFastingRequest_ToCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        var request = new StartFastingHttpRequest("F16_8", 16, "Feeling good");

        var command = request.ToCommand(userId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal("F16_8", command.Protocol);
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
}
