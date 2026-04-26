using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;

namespace FoodDiary.Application.Admin.Commands.DismissContentReport;

public sealed record DismissContentReportCommand(
    Guid ReportId,
    string? AdminNote) : ICommand<Result>;
