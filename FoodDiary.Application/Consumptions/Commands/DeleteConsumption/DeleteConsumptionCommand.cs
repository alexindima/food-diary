using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;

namespace FoodDiary.Application.Consumptions.Commands.DeleteConsumption;

public record DeleteConsumptionCommand(Guid? UserId, Guid ConsumptionId) : ICommand<Result<bool>>, IUserRequest;
