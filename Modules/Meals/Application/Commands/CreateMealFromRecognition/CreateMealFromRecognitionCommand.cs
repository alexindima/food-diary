using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Meals.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Meals.Commands.CreateMealFromRecognition;

public sealed record CreateMealFromRecognitionCommand(Guid? UserId, Guid RecognitionId, DateTime OccurredAtUtc)
    : ICommand<Result<RecognizedMealCreationModel>>, IUserRequest;
