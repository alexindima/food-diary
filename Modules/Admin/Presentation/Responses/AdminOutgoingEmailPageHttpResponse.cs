namespace FoodDiary.Modules.Admin.Presentation.Responses;

public sealed record AdminOutgoingEmailPageHttpResponse(IReadOnlyList<AdminOutgoingEmailHttpResponse> Items, long TotalItems, IReadOnlyDictionary<string, long>? StatusCounts = null);
