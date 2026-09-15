using FoodDiary.Modules.ContentReports.Domain.Contracts.Enums;
using FoodDiary.Modules.ContentReports.Contracts.Models;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.ContentReports.Contracts.Queries.GetContentReportsForAdministration;

public sealed record GetContentReportsForAdministrationQuery(
    ReportStatus? Status,
    int Page,
    int Limit,
    ContentReportAdminFilter? Filter = null) : IRequest<(IReadOnlyList<ContentReportAdminReadModel> Items, int Total)>;
