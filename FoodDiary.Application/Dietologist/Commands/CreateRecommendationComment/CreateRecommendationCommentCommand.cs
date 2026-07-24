using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Dietologist.Commands.CreateRecommendationComment;

public sealed record CreateRecommendationCommentCommand(
    Guid? UserId,
    Guid RecommendationId,
    string Text) : ICommand<Result<RecommendationCommentModel>>, IUserRequest;
