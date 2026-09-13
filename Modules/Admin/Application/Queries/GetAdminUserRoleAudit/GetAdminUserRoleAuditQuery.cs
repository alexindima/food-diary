using FoodDiary.Modules.Admin.Application.Abstractions.Models;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;

namespace FoodDiary.Application.Admin.Queries.GetAdminUserRoleAudit;

public sealed record GetAdminUserRoleAuditQuery(Guid UserId, int Limit)
    : IQuery<Result<IReadOnlyList<AdminUserRoleAuditEventReadModel>>>;
