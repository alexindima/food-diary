namespace FoodDiary.Presentation.Api.Features.Admin.Requests;

public sealed record GetAdminRetentionHttpQuery(DateOnly? From, DateOnly? To);
