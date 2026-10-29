using FoodDiary.Modules.Hydration.Presentation.Contracts.Responses;

namespace FoodDiary.Modules.Dashboard.Presentation.Contracts.Responses;

public sealed record DashboardHydrationHistoryHttpResponse(
    IReadOnlyList<HydrationEntryHttpResponse> Entries,
    bool HasMore);
