using FoodDiary.Application.WeeklyGoals.Commands.UpsertWeeklyGoal;
using FoodDiary.Application.WeeklyGoals.Models;
using FoodDiary.Application.WeeklyGoals.Queries.GetWeeklyGoal;
using FoodDiary.Presentation.Api.Features.WeeklyGoals.Requests;
using FoodDiary.Presentation.Api.Features.WeeklyGoals.Responses;

namespace FoodDiary.Presentation.Api.Features.WeeklyGoals.Mappings;

public static class WeeklyGoalHttpMappings {
    extension(GetWeeklyGoalHttpQuery query) {
        public GetWeeklyGoalQuery ToQuery(Guid userId) => new(userId, query.WeekStart);
    }

    extension(UpsertWeeklyGoalHttpRequest request) {
        public UpsertWeeklyGoalCommand ToCommand(Guid userId) =>
            new(
                userId,
                request.WeekStart,
                request.TargetDays,
                request.ReminderEnabled,
                request.ReminderTime,
                request.TimeZoneOffsetMinutes);
    }

    extension(WeeklyGoalModel model) {
        public WeeklyGoalHttpResponse ToHttpResponse() =>
            new(
                model.Id,
                model.WeekStart,
                model.Type,
                model.TargetDays,
                model.ProgressDays,
                model.IsCompleted,
                model.ReminderEnabled,
                model.ReminderTime,
                model.TimeZoneOffsetMinutes);
    }
}
