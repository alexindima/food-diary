namespace FoodDiary.Modules.Admin.Presentation.Requests;

public sealed record AdminDailyAdvicesImportHttpRequest(int Version, IReadOnlyList<AdminDailyAdviceImportItemHttpRequest> Advices);
