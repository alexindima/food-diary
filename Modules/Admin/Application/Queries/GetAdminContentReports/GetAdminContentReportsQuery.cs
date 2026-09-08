using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Application.Admin.Models;

namespace FoodDiary.Application.Admin.Queries.GetAdminContentReports;

public sealed record GetAdminContentReportsQuery(
    string? Status,
    int Page,
    int Limit, DateTime? FromUtc = null, DateTime? ToUtc = null, string? TargetType = null, Guid? ReporterId = null, Guid? TargetId = null) : IQuery<Result<PagedResponse<AdminContentReportModel>>>;
