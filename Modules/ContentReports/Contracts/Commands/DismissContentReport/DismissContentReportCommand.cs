using FoodDiary.Modules.ContentReports.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.ContentReports.Contracts.Commands.DismissContentReport;

// Executes within the caller-owned unit of work; this request does not commit.
public sealed record DismissContentReportCommand(
    ContentReportId ReportId,
    UserId ReviewerUserId,
    string? AdminNote) : IRequest<Result>;
