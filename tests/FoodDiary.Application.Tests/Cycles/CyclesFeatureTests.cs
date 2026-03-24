using FoodDiary.Application.Cycles.Commands.CreateCycle;
using FoodDiary.Application.Cycles.Commands.UpsertCycleDay;
using FoodDiary.Application.Cycles.Mappings;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Application.Cycles.Services;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Cycles;

public class CyclesFeatureTests {
    [Fact]
    public async Task CreateCycleCommandValidator_WithInvalidLength_Fails() {
        var validator = new CreateCycleCommandValidator();
        var command = new CreateCycleCommand(Guid.NewGuid(), DateTime.UtcNow, AverageLength: 10, LutealLength: 20, Notes: null);

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task UpsertCycleDayCommandValidator_WithOutOfRangeSymptoms_Fails() {
        var validator = new UpsertCycleDayCommandValidator();
        var command = new UpsertCycleDayCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow,
            IsPeriod: true,
            Symptoms: new DailySymptomsModel(10, 0, 0, 0, 0, 0, 0),
            Notes: null);

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void CycleMappings_ToModel_SortsDaysByDate() {
        var cycle = Cycle.Create(UserId.New(), DateTime.UtcNow);
        cycle.AddOrUpdateDay(new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc), true, DailySymptoms.Create(1, 1, 1, 1, 1, 1, 1));
        cycle.AddOrUpdateDay(new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), false, DailySymptoms.Create(2, 2, 2, 2, 2, 2, 2));

        var response = cycle.ToModel();

        Assert.Collection(
            response.Days,
            day => Assert.Equal(new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), day.Date),
            day => Assert.Equal(new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc), day.Date));
    }

    [Fact]
    public void CyclePredictionService_CalculatePredictions_NormalizesToUtcDate() {
        var localStart = DateTime.SpecifyKind(new DateTime(2026, 1, 10, 23, 30, 0), DateTimeKind.Local);
        var cycle = Cycle.Create(UserId.New(), localStart, averageLength: 28, lutealLength: 14);

        var predictions = CyclePredictionService.CalculatePredictions(cycle);

        Assert.NotNull(predictions.NextPeriodStart);
        Assert.NotNull(predictions.OvulationDate);
        Assert.NotNull(predictions.PmsStart);
        Assert.Equal(DateTimeKind.Utc, predictions.NextPeriodStart!.Value.Kind);
        Assert.Equal(DateTimeKind.Utc, predictions.OvulationDate!.Value.Kind);
        Assert.Equal(DateTimeKind.Utc, predictions.PmsStart!.Value.Kind);
    }

    [Fact]
    public void CyclePredictionService_CalculatePredictions_WithNullCycle_Throws() {
        Assert.Throws<ArgumentNullException>(() => CyclePredictionService.CalculatePredictions(null!));
    }
}
