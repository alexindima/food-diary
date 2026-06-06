using FoodDiary.Application.Cycles.Commands.UpsertCycleDay;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Presentation.Api.Features.Cycles.Mappings;
using FoodDiary.Presentation.Api.Features.Cycles.Models;
using FoodDiary.Presentation.Api.Features.Cycles.Requests;
using FoodDiary.Presentation.Api.Features.Cycles.Responses;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
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

        UpsertCycleDayCommand command = request.ToCommand(userId, cycleId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(cycleId, command.CycleId);
        Assert.Equal(request.Date, command.Date);
        Assert.Equal(request.IsPeriod, command.IsPeriod);
        Assert.Equal(request.Notes, command.Notes);
        Assert.Equal(request.ClearNotes, command.ClearNotes);
    }

    [Fact]
    public void CycleModel_ToHttpResponse_MapsDaysAndPredictions() {
        var cycleId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var dayId = Guid.NewGuid();
        var startDate = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var model = new CycleModel(
            cycleId,
            userId,
            startDate,
            AverageLength: 29,
            LutealLength: 13,
            Notes: "cycle notes",
            [
                new CycleDayModel(
                    dayId,
                    cycleId,
                    startDate.AddDays(1),
                    IsPeriod: true,
                    new DailySymptomsModel(1, 2, 3, 4, 5, 6, 7),
                    Notes: "day notes"),
            ],
            new CyclePredictionsModel(
                NextPeriodStart: startDate.AddDays(29),
                OvulationDate: startDate.AddDays(15),
                PmsStart: startDate.AddDays(26)));

        CycleHttpResponse response = model.ToHttpResponse();

        Assert.Equal(cycleId, response.Id);
        Assert.Equal(userId, response.UserId);
        Assert.Equal(startDate, response.StartDate);
        Assert.Equal(29, response.AverageLength);
        Assert.Equal(13, response.LutealLength);
        Assert.Equal("cycle notes", response.Notes);
        CycleDayHttpResponse day = Assert.Single(response.Days);
        Assert.Equal(dayId, day.Id);
        Assert.True(day.IsPeriod);
        Assert.Equal(7, day.Symptoms.Libido);
        Assert.Equal("day notes", day.Notes);
        Assert.Equal(startDate.AddDays(29), response.Predictions!.NextPeriodStart);
        Assert.Equal(startDate.AddDays(15), response.Predictions.OvulationDate);
        Assert.Equal(startDate.AddDays(26), response.Predictions.PmsStart);
    }

    [Fact]
    public void CycleModel_ToHttpResponse_MapsNullPredictions() {
        var model = new CycleModel(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow,
            AverageLength: 28,
            LutealLength: 14,
            Notes: null,
            [],
            Predictions: null);

        CycleHttpResponse response = model.ToHttpResponse();

        Assert.Null(response.Predictions);
        Assert.Empty(response.Days);
    }
}
