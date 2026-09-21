namespace FoodDiary.Modules.Users.Presentation.Contracts.Responses;

public sealed record WaistGoalHistoryPageHttpResponse(IReadOnlyList<WaistGoalHistoryHttpResponse> Items, string? NextCursor);
