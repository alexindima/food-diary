using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Commands.DismissContentReport;

public sealed record DismissContentReportCommand(
    Guid ReportId,
    Guid ReviewerUserId,
    string? AdminNote) : ICommand<Result>;
