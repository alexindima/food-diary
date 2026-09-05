using FoodDiary.Application.RecipeCommunity.RecipeLikes.Commands.ToggleRecipeLike;
using FoodDiary.Application.RecipeCommunity.RecipeLikes.Models;
using FoodDiary.Application.RecipeCommunity.RecipeLikes.Queries.GetRecipeLikeStatus;
using FoodDiary.Presentation.Api.Features.RecipeLikes.Responses;

namespace FoodDiary.Presentation.Api.Features.RecipeLikes.Mappings;

public static class RecipeLikeHttpMappings {
    public static ToggleRecipeLikeCommand ToCommand(Guid userId, Guid recipeId, bool isLiked) =>
        new(userId, recipeId, isLiked);

    public static GetRecipeLikeStatusQuery ToQuery(Guid userId, Guid recipeId) =>
        new(userId, recipeId);

    extension(RecipeLikeStatusModel model) {
        public RecipeLikeStatusHttpResponse ToHttpResponse() =>
                new(model.IsLiked, model.TotalLikes);
    }
}
