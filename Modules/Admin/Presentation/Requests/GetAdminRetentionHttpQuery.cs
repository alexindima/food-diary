namespace FoodDiary.Modules.Admin.Presentation.Requests;

public sealed record GetAdminRetentionHttpQuery(DateOnly? From, DateOnly? To, DateOnly? CohortFrom = null, DateOnly? CohortTo = null);
