using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Modules.Admin.Application.Models;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminContentReports;

public sealed record GetAdminContentReportsQuery(
    string? Status,
    int Page,
    int Limit, DateTime? FromUtc = null, DateTime? ToUtc = null, string? TargetType = null, Guid? ReporterId = null, Guid? TargetId = null) : IQuery<Result<PagedResponse<AdminContentReportModel>>>;
