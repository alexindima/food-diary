using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Mediator;

namespace FoodDiary.Application.ContentReports.Commands.ReviewContentReport;

// Executes within the caller-owned unit of work; this request does not commit.
public sealed record ReviewContentReportCommand(
    ContentReportId ReportId,
    UserId ReviewerUserId,
    string? AdminNote) : IRequest<Result>;
