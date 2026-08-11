using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Users.Models;

public sealed record UserAiProfileModel(
    UserId UserId,
    string? Language,
    long InputTokenLimit,
    long OutputTokenLimit);
