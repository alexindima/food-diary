using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Ai.Application.Commands.ParseFoodText;

public record ParseFoodTextCommand(
    Guid? UserId,
    string Text,
    string RequestId) : ICommand<Result<FoodVisionModel>>, IUserRequest;
