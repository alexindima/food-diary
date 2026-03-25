using FoodDiary.Application.Cycles.Models;
using FoodDiary.Presentation.Api.Features.Cycles.Models;
using FoodDiary.Presentation.Api.Features.Cycles.Responses;
using FoodDiary.Presentation.Api.Responses;

namespace FoodDiary.Presentation.Api.Features.Cycles.Mappings;

public static class CycleHttpResponseMappings {
    public static CycleHttpResponse ToHttpResponse(this CycleModel model) {
        return new CycleHttpResponse(
            model.Id,
            model.UserId,
            model.StartDate,
            model.AverageLength,
            model.LutealLength,
            model.Notes,
            model.Days.ToHttpResponseList(ToHttpResponse),
            model.Predictions is null
                ? null
                : new CyclePredictionsHttpResponse(
                    model.Predictions.NextPeriodStart,
                    model.Predictions.OvulationDate,
                    model.Predictions.PmsStart)
        );
    }

    public static CycleDayHttpResponse ToHttpResponse(this CycleDayModel model) {
        return new CycleDayHttpResponse(
            model.Id,
            model.CycleId,
            model.Date,
            model.IsPeriod,
            new DailySymptomsHttpModel(
                model.Symptoms.Pain,
                model.Symptoms.Mood,
                model.Symptoms.Edema,
                model.Symptoms.Headache,
                model.Symptoms.Energy,
                model.Symptoms.SleepQuality,
                model.Symptoms.Libido
            ),
            model.Notes
        );
    }
}
