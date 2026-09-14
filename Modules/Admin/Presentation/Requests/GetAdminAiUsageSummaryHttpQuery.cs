namespace FoodDiary.Modules.Admin.Presentation.Requests;

public sealed record GetAdminAiUsageSummaryHttpQuery(
    DateOnly? From,
    DateOnly? To, Guid? UserId = null);
