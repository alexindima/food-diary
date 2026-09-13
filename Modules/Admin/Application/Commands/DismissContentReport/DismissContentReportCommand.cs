using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Commands.DismissContentReport;

public sealed record DismissContentReportCommand(
    Guid ReportId,
    Guid ReviewerUserId,
    string? AdminNote) : ICommand<Result>;
