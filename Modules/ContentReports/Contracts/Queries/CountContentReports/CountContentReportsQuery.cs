using FoodDiary.Modules.ContentReports.Domain.Contracts.Enums;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.ContentReports.Contracts.Queries.CountContentReports;

public sealed record CountContentReportsQuery(
    ReportStatus Status) : IRequest<int>;
