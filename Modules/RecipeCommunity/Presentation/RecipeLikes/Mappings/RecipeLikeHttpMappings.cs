using FoodDiary.Modules.RecipeCommunity.Application.RecipeLikes.Commands.ToggleRecipeLike;
using FoodDiary.Modules.RecipeCommunity.Application.RecipeLikes.Models;
using FoodDiary.Modules.RecipeCommunity.Application.RecipeLikes.Queries.GetRecipeLikeStatus;
using FoodDiary.Modules.RecipeCommunity.Presentation.RecipeLikes.Responses;

namespace FoodDiary.Modules.RecipeCommunity.Presentation.RecipeLikes.Mappings;

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
