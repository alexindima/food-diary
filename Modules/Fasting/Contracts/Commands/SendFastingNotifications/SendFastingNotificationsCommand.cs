using FoodDiary.Mediator;

namespace FoodDiary.Modules.Fasting.Contracts.Commands.SendFastingNotifications;

public sealed record SendFastingNotificationsCommand : IRequest<int>;
