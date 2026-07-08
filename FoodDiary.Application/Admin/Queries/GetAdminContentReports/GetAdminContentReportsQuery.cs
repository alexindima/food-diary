using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Admin.Models;

namespace FoodDiary.Application.Admin.Queries.GetAdminContentReports;

public sealed record GetAdminContentReportsQuery(
    string? Status,
    int Page,
    int Limit) : IQuery<Result<PagedResponse<AdminContentReportModel>>>;
