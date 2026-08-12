using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Application.RecipeComments.Models;

namespace FoodDiary.Application.RecipeComments.Queries.GetRecipeComments;

public record GetRecipeCommentsQuery(
    Guid? UserId,
    Guid RecipeId,
    int Page,
    int Limit) : IQuery<Result<PagedResponse<RecipeCommentModel>>>, IUserRequest;
