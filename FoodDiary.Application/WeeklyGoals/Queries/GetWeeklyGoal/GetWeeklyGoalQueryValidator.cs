using FluentValidation;

namespace FoodDiary.Application.WeeklyGoals.Queries.GetWeeklyGoal;

public sealed class GetWeeklyGoalQueryValidator : AbstractValidator<GetWeeklyGoalQuery> {
    public GetWeeklyGoalQueryValidator() {
        RuleFor(query => query.WeekStart)
            .Must(static value => value.DayOfWeek == DayOfWeek.Monday)
            .WithMessage("Week start must be a Monday.");
    }
}
