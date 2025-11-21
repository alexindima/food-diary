using System.Collections.Generic;
using System.Linq;
using FoodDiary.Contracts.Cycles;
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Cycles.Mappings;

public static class CycleMappings
{
    public static CycleResponse ToResponse(this Cycle cycle, CyclePredictionsResponse? predictions = null) =>
        new(
            cycle.Id.Value,
            cycle.UserId.Value,
            cycle.StartDate,
            cycle.AverageLength,
            cycle.LutealLength,
            cycle.Notes,
            cycle.Days.Select(d => d.ToResponse()).ToList(),
            predictions);

    public static CycleDayResponse ToResponse(this CycleDay day) =>
        new(
            day.Id.Value,
            day.CycleId.Value,
            day.Date,
            day.IsPeriod,
            day.Symptoms.ToDto(),
            day.Notes);

    public static DailySymptomsDto ToDto(this DailySymptoms symptoms) =>
        new(
            symptoms.Pain,
            symptoms.Mood,
            symptoms.Edema,
            symptoms.Headache,
            symptoms.Energy,
            symptoms.SleepQuality,
            symptoms.Libido);

    public static DailySymptoms ToValueObject(this DailySymptomsDto dto) =>
        DailySymptoms.Create(
            dto.Pain,
            dto.Mood,
            dto.Edema,
            dto.Headache,
            dto.Energy,
            dto.SleepQuality,
            dto.Libido);
}
