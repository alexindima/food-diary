using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;

namespace FoodDiary.Application.Admin.Commands.ReviewContentReport;

public sealed record ReviewContentReportCommand(
    Guid ReportId,
    string? AdminNote) : ICommand<Result>;
