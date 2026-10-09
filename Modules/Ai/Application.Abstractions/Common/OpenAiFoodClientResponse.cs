using FoodDiary.Modules.Ai.Domain.ValueObjects;
namespace FoodDiary.Modules.Ai.Application.Abstractions.Common;

public sealed record OpenAiFoodClientResponse<T>(
    T Value,
    string Operation,
    string Model,
    AiTokenUsage? Usage);
