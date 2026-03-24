namespace FoodDiary.Presentation.Api.Features.Admin.Requests;

public sealed record GetAdminAiUsageSummaryHttpQuery(
    DateOnly? From,
    DateOnly? To);
