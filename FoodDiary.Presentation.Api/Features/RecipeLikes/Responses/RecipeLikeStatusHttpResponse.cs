namespace FoodDiary.Presentation.Api.Features.RecipeLikes.Responses;

public sealed record RecipeLikeStatusHttpResponse(
    bool IsLiked,
    int TotalLikes);
