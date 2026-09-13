using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Commands.ReviewContentReport;

public sealed record ReviewContentReportCommand(
    Guid ReportId,
    Guid ReviewerUserId,
    string? AdminNote) : ICommand<Result>;
