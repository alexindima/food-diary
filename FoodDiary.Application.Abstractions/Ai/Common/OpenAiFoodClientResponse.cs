namespace FoodDiary.Application.Abstractions.Ai.Common;

public sealed record OpenAiFoodClientResponse<T>(
    T Value,
    string Operation,
    string Model,
    AiUsageTokens? Usage);
