namespace FoodDiary.Application.RecipeCommunity.RecipeLikes.Models;

public sealed record RecipeLikeStatusModel(
    bool IsLiked,
    int TotalLikes);
