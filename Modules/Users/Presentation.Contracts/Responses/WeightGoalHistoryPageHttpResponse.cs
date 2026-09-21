namespace FoodDiary.Modules.Users.Presentation.Contracts.Responses;

public sealed record WeightGoalHistoryPageHttpResponse(IReadOnlyList<WeightGoalHistoryHttpResponse> Items, string? NextCursor);
