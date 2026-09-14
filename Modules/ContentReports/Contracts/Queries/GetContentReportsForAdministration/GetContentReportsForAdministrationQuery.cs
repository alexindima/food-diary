using FoodDiary.Application.ContentReports.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Mediator;

namespace FoodDiary.Application.ContentReports.Queries.GetContentReportsForAdministration;

public sealed record GetContentReportsForAdministrationQuery(
    ReportStatus? Status,
    int Page,
    int Limit,
    ContentReportAdminFilter? Filter = null) : IRequest<(IReadOnlyList<ContentReportAdminReadModel> Items, int Total)>;
