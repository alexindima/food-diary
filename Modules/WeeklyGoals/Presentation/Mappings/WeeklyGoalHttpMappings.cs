using FoodDiary.Modules.WeeklyGoals.Application.Commands.UpsertWeeklyGoal;
using FoodDiary.Modules.WeeklyGoals.Contracts.Models;
using FoodDiary.Modules.WeeklyGoals.Application.Queries.GetWeeklyGoal;
using FoodDiary.Modules.WeeklyGoals.Presentation.Requests;
using FoodDiary.Modules.WeeklyGoals.Presentation.Responses;

namespace FoodDiary.Modules.WeeklyGoals.Presentation.Mappings;

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
