using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Ai.Commands.ParseFoodText;

public record ParseFoodTextCommand(
    Guid? UserId,
    string Text,
    string RequestId) : ICommand<Result<FoodVisionModel>>, IUserRequest;
