namespace FoodDiary.Modules.Admin.Presentation.Features.Admin.Requests;

public sealed record GetAdminAiUsageSummaryHttpQuery(
    DateOnly? From,
    DateOnly? To, Guid? UserId = null);
