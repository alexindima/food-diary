namespace FoodDiary.Modules.RecipeCommunity.Application.RecipeLikes.Models;

public sealed record RecipeLikeStatusModel(
    bool IsLiked,
    int TotalLikes);
