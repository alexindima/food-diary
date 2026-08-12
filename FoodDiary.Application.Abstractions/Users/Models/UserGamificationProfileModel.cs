using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Abstractions.Users.Models;

public sealed record UserGamificationProfileModel(UserCalorieSchedule CalorieSchedule);
