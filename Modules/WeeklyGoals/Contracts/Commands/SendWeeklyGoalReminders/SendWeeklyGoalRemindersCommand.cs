using FoodDiary.Mediator;

namespace FoodDiary.Modules.WeeklyGoals.Contracts.Commands.SendWeeklyGoalReminders;

public sealed record SendWeeklyGoalRemindersCommand : IRequest<int>;
