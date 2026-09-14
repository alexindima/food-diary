using FoodDiary.Domain.Enums;
using FoodDiary.Mediator;

namespace FoodDiary.Application.ContentReports.Queries.CountContentReports;

public sealed record CountContentReportsQuery(
    ReportStatus Status) : IRequest<int>;
