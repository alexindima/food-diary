using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Queries.GetCollaborationAudit;

public sealed record GetCollaborationAuditQuery(Guid? ClientUserId, int Limit)
    : IQuery<Result<IReadOnlyList<AdminAuditEntryModel>>>;
