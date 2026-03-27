using FoodDiary.Presentation.Api.Features.Cycles.Mappings;
using FoodDiary.Presentation.Api.Features.Cycles.Models;
using FoodDiary.Presentation.Api.Features.Cycles.Requests;

namespace FoodDiary.Presentation.Api.Tests;

public sealed class CycleHttpMappingsTests {
    [Fact]
    public void UpsertCycleDayRequest_ToCommand_MapsClearNotes() {
        var userId = Guid.NewGuid();
        var cycleId = Guid.NewGuid();
        var request = new UpsertCycleDayHttpRequest(
            Date: new DateTime(2026, 3, 27, 0, 0, 0, DateTimeKind.Utc),
            IsPeriod: true,
            Symptoms: new DailySymptomsHttpModel(1, 2, 3, 4, 5, 6, 7),
            Notes: null,
            ClearNotes: true);

        var command = request.ToCommand(userId, cycleId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(cycleId, command.CycleId);
        Assert.Equal(request.Date, command.Date);
        Assert.Equal(request.IsPeriod, command.IsPeriod);
        Assert.Equal(request.Notes, command.Notes);
        Assert.Equal(request.ClearNotes, command.ClearNotes);
    }
}
