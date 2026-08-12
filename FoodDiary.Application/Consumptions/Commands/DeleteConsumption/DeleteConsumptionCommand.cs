using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Consumptions.Commands.DeleteConsumption;

public record DeleteConsumptionCommand(Guid? UserId, Guid ConsumptionId) : ICommand<Result>, IUserRequest;
