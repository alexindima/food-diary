using FoodDiary.Modules.Cycles.Application.Abstractions.Models;
using FoodDiary.Modules.Cycles.Application.Services;
using FoodDiary.Modules.Cycles.Contracts.Models;
using FoodDiary.Modules.Cycles.Domain.Contracts.Enums;
using FoodDiary.Modules.Cycles.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Cycles.Application.Tests;

public partial class CyclesFeatureTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CyclePredictionService_SparseLogsWithinConfirmedRanges_DoNotCreateExtraStartsAsync(bool excludeSecond) {
        CycleProfile profile = CreateConfirmedHistory(excludeSecond, logEndDays: true);
        Assert.Equal(4, profile.MenstrualEpisodes.Count);
        CycleProfileReadModel readModel = Assert.IsType<CycleProfileReadModel>(
            await new InMemoryCycleRepository(profile).GetCurrentReadModelAsync(profile.UserId));

        CyclePredictionsModel aggregatePrediction = CyclePredictionService.CalculatePredictions(profile, new DateOnly(2026, 9, 23));
        CyclePredictionsModel readPrediction = CyclePredictionService.CalculatePredictions(readModel, new DateOnly(2026, 9, 23));

        foreach (CyclePredictionsModel prediction in new[] { aggregatePrediction, readPrediction }) {
            Assert.Equal(excludeSecond ? 3 : 4, prediction.UsedEpisodeCount);
            Assert.Equal(excludeSecond ? 1 : 0, prediction.ExcludedEpisodeCount);
            Assert.Equal(excludeSecond ? 2 : 3, prediction.CompletedCycleCount);
            Assert.DoesNotContain("ambiguous_episode_history", prediction.ReasonCodes, StringComparer.Ordinal);
            if (excludeSecond) {
                Assert.Null(prediction.NextPeriodStartFrom);
                Assert.Contains("insufficient_completed_cycles", prediction.ReasonCodes, StringComparer.Ordinal);
            } else {
                Assert.Equal(new DateOnly(2026, 10, 19), prediction.NextPeriodStartFrom);
                Assert.Equal(new DateOnly(2026, 10, 23), prediction.NextPeriodStartTo);
            }
        }
    }

    [Fact]
    public async Task CyclePredictionService_InferredGroupRunningIntoConfirmedRange_UsesConfirmedStartOnceAsync() {
        CycleProfile profile = CreateConfirmedHistory(excludeSecond: false, logEndDays: false);
        profile.UpsertBleedingEntry(new DateOnly(2026, 6, 27), BleedingType.Bleeding, CycleFlowLevel.Medium, painImpact: null, notes: null);
        profile.UpsertBleedingEntry(new DateOnly(2026, 6, 29), BleedingType.Bleeding, CycleFlowLevel.Medium, painImpact: null, notes: null);
        Assert.Equal(4, profile.MenstrualEpisodes.Count);
        CycleProfileReadModel readModel = Assert.IsType<CycleProfileReadModel>(
            await new InMemoryCycleRepository(profile).GetCurrentReadModelAsync(profile.UserId));

        CyclePredictionsModel aggregatePrediction = CyclePredictionService.CalculatePredictions(profile, new DateOnly(2026, 9, 23));
        CyclePredictionsModel readPrediction = CyclePredictionService.CalculatePredictions(readModel, new DateOnly(2026, 9, 23));

        foreach (CyclePredictionsModel prediction in new[] { aggregatePrediction, readPrediction }) {
            Assert.Equal(4, prediction.UsedEpisodeCount);
            Assert.Equal(3, prediction.CompletedCycleCount);
            Assert.Equal(new DateOnly(2026, 10, 19), prediction.NextPeriodStartFrom);
            Assert.Equal(new DateOnly(2026, 10, 23), prediction.NextPeriodStartTo);
        }
    }

    [Theory]
    [InlineData(6, false)]
    [InlineData(7, true)]
    public async Task CyclePredictionService_SeparateInferredGroup_RespectsConfirmedRangeToleranceAsync(int offsetDays, bool separate) {
        CycleProfile profile = CreateConfirmedHistory(excludeSecond: false, logEndDays: false);
        profile.UpsertBleedingEntry(new DateOnly(2026, 7, 1).AddDays(offsetDays), BleedingType.Bleeding, CycleFlowLevel.Medium, painImpact: null, notes: null);
        Assert.Equal(separate ? 5 : 4, profile.MenstrualEpisodes.Count);
        CycleProfileReadModel readModel = Assert.IsType<CycleProfileReadModel>(
            await new InMemoryCycleRepository(profile).GetCurrentReadModelAsync(profile.UserId));

        CyclePredictionsModel aggregatePrediction = CyclePredictionService.CalculatePredictions(profile, new DateOnly(2026, 9, 23));
        CyclePredictionsModel readPrediction = CyclePredictionService.CalculatePredictions(readModel, new DateOnly(2026, 9, 23));

        foreach (CyclePredictionsModel prediction in new[] { aggregatePrediction, readPrediction }) {
            Assert.Equal(separate ? 5 : 4, prediction.UsedEpisodeCount);
            Assert.Equal(separate, prediction.ReasonCodes.Contains("ambiguous_episode_history", StringComparer.Ordinal));
            Assert.Equal(separate, prediction.NextPeriodStartFrom is null);
        }
    }

    private static CycleProfile CreateConfirmedHistory(bool excludeSecond, bool logEndDays) {
        DateOnly firstStart = new(2026, 7, 1);
        var profile = CycleProfile.Create(UserId.New(), firstStart);
        for (int index = 0; index < 4; index++) {
            DateOnly start = firstStart.AddDays(index * 28);
            profile.UpsertBleedingEntry(start, BleedingType.Bleeding, CycleFlowLevel.Medium, painImpact: null, notes: null);
            MenstrualEpisode episode = profile.ConfirmPeriodStart(start);
            profile.UpdateMenstrualEpisode(episode.Id, start, start.AddDays(4), excludeSecond && index == 1);
            if (logEndDays) {
                profile.UpsertBleedingEntry(start.AddDays(4), BleedingType.Bleeding, CycleFlowLevel.Medium, painImpact: null, notes: null);
            }
        }
        return profile;
    }
}
