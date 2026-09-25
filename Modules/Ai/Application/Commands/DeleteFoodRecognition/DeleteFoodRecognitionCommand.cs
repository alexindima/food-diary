using FoodDiary.Mediator;
using FoodDiary.Results;

namespace FoodDiary.Modules.Ai.Application.Commands.DeleteFoodRecognition;

public sealed record DeleteFoodRecognitionCommand(Guid UserId, Guid Id) : IRequest<Result>;
