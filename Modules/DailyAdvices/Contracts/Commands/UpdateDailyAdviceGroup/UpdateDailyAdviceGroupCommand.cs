using FoodDiary.Mediator;
using FoodDiary.Results;
using FoodDiary.Modules.DailyAdvices.Contracts.Models;

namespace FoodDiary.Modules.DailyAdvices.Contracts.Commands.UpdateDailyAdviceGroup;

public sealed record UpdateDailyAdviceGroupCommand(Guid Id, string Ru, string En, int Weight, string? Tag) : IRequest<Result<DailyAdviceGroupModel>>;
