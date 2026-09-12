using FoodDiary.Application.Meals.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Meals.Commands.UndoRecognizedMeal;

public sealed record UndoRecognizedMealCommand(Guid? UserId, Guid OperationId)
    : ICommand<Result<RecognizedMealUndoModel>>, IUserRequest;
