using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Dietologist.Application.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Dietologist.Application.Commands.CreateRecommendationComment;

public sealed record CreateRecommendationCommentCommand(
    Guid? UserId,
    Guid RecommendationId,
    string Text) : ICommand<Result<RecommendationCommentModel>>, IUserRequest;
