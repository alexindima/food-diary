namespace FoodDiary.Modules.Admin.Presentation.Features.Admin.Responses;

public sealed record AdminOutgoingEmailPageHttpResponse(IReadOnlyList<AdminOutgoingEmailHttpResponse> Items, long TotalItems, IReadOnlyDictionary<string, long>? StatusCounts = null);
