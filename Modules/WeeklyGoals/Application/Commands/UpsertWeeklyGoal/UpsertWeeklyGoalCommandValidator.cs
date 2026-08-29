using FluentValidation;
using FoodDiary.Application.WeeklyGoals.Common;

namespace FoodDiary.Application.WeeklyGoals.Commands.UpsertWeeklyGoal;

public sealed class UpsertWeeklyGoalCommandValidator : AbstractValidator<UpsertWeeklyGoalCommand> {
    private static readonly int[] SupportedTargets = [3, 5, 7];

    public UpsertWeeklyGoalCommandValidator(TimeProvider timeProvider) {
        RuleFor(command => command.WeekStart)
            .Must(static value => value.DayOfWeek == DayOfWeek.Monday)
            .WithMessage("Week start must be a Monday.");
        RuleFor(command => command.WeekStart)
            .Must(value => WeeklyGoalWeekPolicy.CanWrite(value, timeProvider.GetUtcNow().UtcDateTime))
            .WithMessage("A weekly goal can be changed only for an adjacent current week.");
        RuleFor(command => command.TargetDays)
            .Must(static value => SupportedTargets.Contains(value))
            .WithMessage("Target days must be 3, 5, or 7.");
        When(command => command.ReminderEnabled, () => {
            RuleFor(command => command.ReminderTime).NotNull();
            RuleFor(command => command.TimeZoneOffsetMinutes).NotNull().InclusiveBetween(-14 * 60, 14 * 60);
        });
    }
}
