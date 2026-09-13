using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Queries.GetCollaborationAudit;

public sealed record GetCollaborationAuditQuery(Guid? ClientUserId, int Limit)
    : IQuery<Result<IReadOnlyList<AdminAuditEntryModel>>>;
