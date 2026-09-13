namespace FoodDiary.Modules.Admin.Presentation.Features.Admin.Requests;

public sealed record GetAdminRetentionHttpQuery(DateOnly? From, DateOnly? To);
