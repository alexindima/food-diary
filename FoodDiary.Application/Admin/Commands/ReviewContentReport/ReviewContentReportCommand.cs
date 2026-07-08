using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Commands.ReviewContentReport;

public sealed record ReviewContentReportCommand(
    Guid ReportId,
    string? AdminNote) : ICommand<Result>;
