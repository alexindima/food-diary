using FoodDiary.Application.Cycles.Commands.UpsertCycleFactor;
using FoodDiary.Application.Cycles.Commands.UpsertCycleDay;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Presentation.Api.Features.Cycles.Mappings;
using FoodDiary.Presentation.Api.Features.Cycles.Requests;
using FoodDiary.Presentation.Api.Features.Cycles.Responses;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class CycleHttpMappingsTests {
    [Fact]
    public void UpsertCycleDayRequest_ToCommand_MapsClearNotes() {
        var userId = Guid.NewGuid();
        var cycleProfileId = Guid.NewGuid();
        var request = new UpsertCycleDayHttpRequest(
            Date: new DateTime(2026, 3, 27, 0, 0, 0, DateTimeKind.Utc),
            Bleeding: new BleedingLogHttpModel((int)BleedingType.Bleeding, (int)CycleFlowLevel.Medium, PainImpact: 2, Notes: null, ClearNotes: true),
            Symptoms: [new SymptomLogHttpModel((int)CycleSymptomCategory.Pain, 4, ["cramp"], Note: null, ClearNote: false)],
            FertilitySignal: null);

        UpsertCycleDayCommand command = request.ToCommand(userId, cycleProfileId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(cycleProfileId, command.CycleProfileId);
        Assert.Equal(request.Date, command.Date);
        BleedingLogHttpModel bleeding = request.Bleeding!;
        Assert.Equal(bleeding.Type, command.Bleeding!.Type);
        Assert.Equal(bleeding.ClearNotes, command.Bleeding.ClearNotes);
        Assert.Single(command.Symptoms);
    }

    [Fact]
    public void UpsertCycleFactorRequest_ToCommand_MapsClearNotes() {
        var userId = Guid.NewGuid();
        var cycleProfileId = Guid.NewGuid();
        var request = new UpsertCycleFactorHttpRequest(
            Type: (int)CycleFactorType.HormonalContraception,
            StartDate: new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate: null,
            Notes: null,
            ClearNotes: true);

        UpsertCycleFactorCommand command = request.ToCommand(userId, cycleProfileId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(cycleProfileId, command.CycleProfileId);
        Assert.Equal(request.Type, command.Type);
        Assert.Equal(request.StartDate, command.StartDate);
        Assert.True(command.ClearNotes);
    }

    [Fact]
    public void CycleModel_ToHttpResponse_MapsDaysAndPredictions() {
        var cycleId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var bleedingId = Guid.NewGuid();
        var startDate = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var model = new CycleModel(
            cycleId,
            userId,
            CycleTrackingMode.PeriodTracking,
            CycleConfidence.Medium,
            startDate,
            AverageCycleLength: 29,
            AveragePeriodLength: 5,
            LutealLength: 13,
            IsRegular: true,
            IsOnboardingComplete: true,
            ShowFertilityEstimates: true,
            DiscreetNotifications: true,
            Notes: "cycle notes",
            [
                new BleedingEntryModel(
                    bleedingId,
                    cycleId,
                    startDate.AddDays(1),
                    BleedingType.Bleeding,
                    CycleFlowLevel.Medium,
                    PainImpact: 3,
                    Notes: "day notes"),
            ],
            Symptoms: [],
            Factors: [],
            FertilitySignals: [],
            new CyclePredictionsModel(
                startDate.AddDays(28),
                startDate.AddDays(30),
                startDate.AddDays(14),
                startDate.AddDays(16),
                startDate.AddDays(24),
                startDate.AddDays(30),
                "Medium",
                "test"));

        CycleHttpResponse response = model.ToHttpResponse();

        Assert.Equal(cycleId, response.Id);
        Assert.Equal(userId, response.UserId);
        Assert.Equal(startDate, response.TrackingStartDate);
        Assert.Equal(29, response.AverageCycleLength);
        Assert.Equal(13, response.LutealLength);
        Assert.Equal("cycle notes", response.Notes);
        BleedingEntryHttpResponse day = Assert.Single(response.BleedingEntries);
        Assert.Equal(bleedingId, day.Id);
        Assert.Equal((int)CycleFlowLevel.Medium, day.Flow);
        Assert.Equal("day notes", day.Notes);
        Assert.Equal(startDate.AddDays(28), response.Predictions!.NextPeriodStartFrom);
        Assert.Equal(startDate.AddDays(16), response.Predictions.OvulationTo);
        Assert.Equal("Medium", response.Predictions.Confidence);
    }

    [Fact]
    public void CycleModel_ToHttpResponse_MapsNullPredictions() {
        var model = new CycleModel(
            Guid.NewGuid(),
            Guid.NewGuid(),
            CycleTrackingMode.PeriodTracking,
            CycleConfidence.Learning,
            DateTime.UtcNow,
            AverageCycleLength: 28,
            AveragePeriodLength: 5,
            LutealLength: 14,
            IsRegular: false,
            IsOnboardingComplete: false,
            ShowFertilityEstimates: false,
            DiscreetNotifications: true,
            Notes: null,
            [],
            [],
            [],
            [],
            Predictions: null);

        CycleHttpResponse response = model.ToHttpResponse();

        Assert.Null(response.Predictions);
        Assert.Empty(response.BleedingEntries);
    }
}
