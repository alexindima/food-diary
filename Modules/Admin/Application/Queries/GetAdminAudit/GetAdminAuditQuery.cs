using FoodDiary.Application.Abstractions.Audit.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Queries.GetAdminAudit;

public sealed record GetAdminAuditQuery(AuditEntryFilter Filter) : IQuery<Result<AdminAuditPage>>;
