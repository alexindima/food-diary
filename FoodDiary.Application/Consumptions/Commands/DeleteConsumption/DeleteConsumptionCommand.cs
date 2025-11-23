using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Consumptions.Commands.DeleteConsumption;

public record DeleteConsumptionCommand(UserId? UserId, MealId ConsumptionId) : ICommand<Result<bool>>;
