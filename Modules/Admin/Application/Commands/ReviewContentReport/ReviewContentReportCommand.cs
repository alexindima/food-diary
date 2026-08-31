using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Commands.ReviewContentReport;

public sealed record ReviewContentReportCommand(
    Guid ReportId,
    Guid ReviewerUserId,
    string? AdminNote) : ICommand<Result>;
