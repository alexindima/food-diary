using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Ai.Commands.ParseFoodText;

public record ParseFoodTextCommand(
    Guid? UserId,
    string Text) : ICommand<Result<FoodVisionModel>>, IUserRequest;
