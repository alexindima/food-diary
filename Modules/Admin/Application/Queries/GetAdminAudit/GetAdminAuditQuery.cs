using FoodDiary.Audit.Contracts.Audit.Models;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminAudit;

public sealed record GetAdminAuditQuery(AuditEntryFilter Filter) : IQuery<Result<AdminAuditPage>>;
