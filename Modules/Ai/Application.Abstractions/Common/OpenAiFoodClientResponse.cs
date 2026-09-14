namespace FoodDiary.Modules.Ai.Application.Abstractions.Common;

public sealed record OpenAiFoodClientResponse<T>(
    T Value,
    string Operation,
    string Model,
    AiUsageTokens? Usage);
