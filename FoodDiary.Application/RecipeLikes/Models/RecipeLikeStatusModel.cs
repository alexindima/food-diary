namespace FoodDiary.Application.RecipeLikes.Models;

public sealed record RecipeLikeStatusModel(
    bool IsLiked,
    int TotalLikes);
