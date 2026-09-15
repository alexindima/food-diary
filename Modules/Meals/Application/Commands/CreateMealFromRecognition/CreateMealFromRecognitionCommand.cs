using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Meals.Application.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Meals.Application.Commands.CreateMealFromRecognition;

public sealed record CreateMealFromRecognitionCommand(Guid? UserId, Guid RecognitionId, DateTime OccurredAtUtc)
    : ICommand<Result<RecognizedMealCreationModel>>, IUserRequest;
