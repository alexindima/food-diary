namespace FoodDiary.Presentation.Api.Features.Admin.Requests;

public sealed record GetCollaborationAuditHttpQuery(Guid? ClientUserId = null, int Limit = 100);
