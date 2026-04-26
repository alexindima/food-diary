using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.ContentReports.Common;
using FoodDiary.Application.ContentReports.Models;
using FoodDiary.Domain.Entities.Social;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.ContentReports.Commands.CreateContentReport;

public class CreateContentReportCommandHandler(IContentReportRepository reportRepository)
    : ICommandHandler<CreateContentReportCommand, Result<ContentReportModel>> {
    public async Task<Result<ContentReportModel>> Handle(
        CreateContentReportCommand command,
        CancellationToken cancellationToken) {
        var userIdResult = UserIdParser.Parse(command.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<ContentReportModel>(userIdResult.Error);
        }

        var targetType = Enum.Parse<ReportTargetType>(command.TargetType);

        var alreadyReported = await reportRepository.HasUserReportedAsync(
            userIdResult.Value, targetType, command.TargetId, cancellationToken);

        if (alreadyReported) {
            return Result.Failure<ContentReportModel>(Errors.ContentReport.AlreadyReported);
        }

        var report = ContentReport.Create(userIdResult.Value, targetType, command.TargetId, command.Reason);
        await reportRepository.AddAsync(report, cancellationToken);

        return Result.Success(new ContentReportModel(
            report.Id.Value,
            report.UserId.Value,
            report.TargetType.ToString(),
            report.TargetId,
            report.Reason,
            report.Status.ToString(),
            report.AdminNote,
            report.CreatedOnUtc,
            report.ReviewedAtUtc));
    }
}
