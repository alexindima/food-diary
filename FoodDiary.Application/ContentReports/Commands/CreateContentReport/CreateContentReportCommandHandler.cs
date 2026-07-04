using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.ContentReports.Common;
using FoodDiary.Application.ContentReports.Models;
using FoodDiary.Domain.Entities.Social;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.ContentReports.Commands.CreateContentReport;

public class CreateContentReportCommandHandler(IContentReportWriteRepository reportRepository)
    : ICommandHandler<CreateContentReportCommand, Result<ContentReportModel>> {
    public async Task<Result<ContentReportModel>> Handle(
        CreateContentReportCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(command.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<ContentReportModel>(userIdResult.Error);
        }

        Result<ReportTargetType> targetTypeResult = EnumValueParser.ParseRequired<ReportTargetType>(
            command.TargetType,
            nameof(command.TargetType),
            "Target type must be 'Recipe' or 'Comment'.");
        if (targetTypeResult.IsFailure) {
            return Result.Failure<ContentReportModel>(targetTypeResult.Error);
        }

        bool alreadyReported = await reportRepository.HasUserReportedAsync(
            userIdResult.Value, targetTypeResult.Value, command.TargetId, cancellationToken).ConfigureAwait(false);

        if (alreadyReported) {
            return Result.Failure<ContentReportModel>(Errors.ContentReport.AlreadyReported);
        }

        var report = ContentReport.Create(userIdResult.Value, targetTypeResult.Value, command.TargetId, command.Reason);
        await reportRepository.AddAsync(report, cancellationToken).ConfigureAwait(false);

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
