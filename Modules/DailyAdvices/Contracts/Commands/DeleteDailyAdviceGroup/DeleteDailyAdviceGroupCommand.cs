using FoodDiary.Mediator;
using FoodDiary.Results;

namespace FoodDiary.Modules.DailyAdvices.Contracts.Commands.DeleteDailyAdviceGroup;

public sealed record DeleteDailyAdviceGroupCommand(Guid Id) : IRequest<Result>;
