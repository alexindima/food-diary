namespace FoodDiary.Modules.RecipeCommunity.Presentation.RecipeLikes.Responses;

public sealed record RecipeLikeStatusHttpResponse(
    bool IsLiked,
    int TotalLikes);
