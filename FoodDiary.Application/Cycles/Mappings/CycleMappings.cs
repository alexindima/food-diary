using FoodDiary.Application.Cycles.Models;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Cycles.Mappings;

public static class CycleMappings {
    public static CycleModel ToModel(this Cycle cycle, CyclePredictionsModel? predictions = null) =>
        new(
            cycle.Id.Value,
            cycle.UserId.Value,
            cycle.StartDate,
            cycle.AverageLength,
            cycle.LutealLength,
            cycle.Notes,
            cycle.Days.OrderBy(d => d.Date).Select(d => d.ToModel()).ToList(),
            predictions);

    public static CycleDayModel ToModel(this CycleDay day) =>
        new(
            day.Id.Value,
            day.CycleId.Value,
            day.Date,
            day.IsPeriod,
            day.Symptoms.ToModel(),
            day.Notes);

    public static DailySymptomsModel ToModel(this DailySymptoms symptoms) =>
        new(
            symptoms.Pain,
            symptoms.Mood,
            symptoms.Edema,
            symptoms.Headache,
            symptoms.Energy,
            symptoms.SleepQuality,
            symptoms.Libido);

    public static DailySymptoms ToValueObject(this DailySymptomsModel model) =>
        DailySymptoms.Create(
            model.Pain,
            model.Mood,
            model.Edema,
            model.Headache,
            model.Energy,
            model.SleepQuality,
            model.Libido);
}
