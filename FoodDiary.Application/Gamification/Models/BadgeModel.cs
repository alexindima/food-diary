namespace FoodDiary.Application.Gamification.Models;

public sealed record BadgeModel(
    string Key,
    string Category,
    int Threshold,
    bool IsEarned);
