namespace FoodDiary.Modules.Admin.Presentation.Requests;

public sealed record AdminDailyAdvicePairsImportHttpRequest(int Version, IReadOnlyList<AdminDailyAdvicePairHttpRequest> Advices);
