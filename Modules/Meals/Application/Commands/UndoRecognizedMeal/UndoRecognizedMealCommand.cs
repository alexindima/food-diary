using FoodDiary.Modules.Meals.Application.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Meals.Application.Commands.UndoRecognizedMeal;

public sealed record UndoRecognizedMealCommand(Guid? UserId, Guid OperationId)
    : ICommand<Result<RecognizedMealUndoModel>>, IUserRequest;
