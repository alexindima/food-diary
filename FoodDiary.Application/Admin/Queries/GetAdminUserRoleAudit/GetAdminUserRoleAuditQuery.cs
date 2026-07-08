using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;

namespace FoodDiary.Application.Admin.Queries.GetAdminUserRoleAudit;

public sealed record GetAdminUserRoleAuditQuery(Guid UserId, int Limit)
    : IQuery<Result<IReadOnlyList<AdminUserRoleAuditEventReadModel>>>;
