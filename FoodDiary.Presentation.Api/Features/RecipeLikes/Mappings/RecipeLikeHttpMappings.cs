using FoodDiary.Application.RecipeLikes.Commands.ToggleRecipeLike;
using FoodDiary.Application.RecipeLikes.Models;
using FoodDiary.Application.RecipeLikes.Queries.GetRecipeLikeStatus;
using FoodDiary.Presentation.Api.Features.RecipeLikes.Responses;

namespace FoodDiary.Presentation.Api.Features.RecipeLikes.Mappings;

public static class RecipeLikeHttpMappings {
    public static ToggleRecipeLikeCommand ToCommand(Guid userId, Guid recipeId) =>
        new(userId, recipeId);

    public static GetRecipeLikeStatusQuery ToQuery(Guid userId, Guid recipeId) =>
        new(userId, recipeId);

    public static RecipeLikeStatusHttpResponse ToHttpResponse(this RecipeLikeStatusModel model) =>
        new(model.IsLiked, model.TotalLikes);
}
