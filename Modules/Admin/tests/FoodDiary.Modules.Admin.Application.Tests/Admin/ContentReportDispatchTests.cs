using FoodDiary.Mediator;
using FoodDiary.Modules.Admin.Application.Commands.ReviewContentReport;
using FoodDiary.Modules.Admin.Application.Commands.DismissContentReport;
using FoodDiary.Results;
using OwnerReview = FoodDiary.Modules.ContentReports.Contracts.Commands.ReviewContentReport.ReviewContentReportCommand;
using OwnerDismiss = FoodDiary.Modules.ContentReports.Contracts.Commands.DismissContentReport.DismissContentReportCommand;

namespace FoodDiary.Modules.Admin.Application.Tests.Admin;

[ExcludeFromCodeCoverage]
public sealed class ContentReportDispatchTests {
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Moderation_ForwardsOwnerRequestTokenAndResult(bool dismiss, bool failure) {
        ISender sender = Substitute.For<ISender>();
        var reportId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        const string note = "  preserve note  ";
        using var cancellation = new CancellationTokenSource();
        Result expected = failure ? Result.Failure(new Error("ContentReport.AlreadyResolved", "Resolved", Kind: ErrorKind.Conflict)) : Result.Success();
        sender.Send(Arg.Any<IRequest<Result>>(), Arg.Any<CancellationToken>()).Returns(expected);

        Result actual = dismiss
            ? await new DismissContentReportCommandHandler(sender).Handle(new DismissContentReportCommand(reportId, reviewerId, note), cancellation.Token)
            : await new ReviewContentReportCommandHandler(sender).Handle(new ReviewContentReportCommand(reportId, reviewerId, note), cancellation.Token);

        Assert.Same(expected, actual);
        await sender.Received(1).Send(Arg.Is<IRequest<Result>>(request => dismiss
            ? request is OwnerDismiss && ((OwnerDismiss)request).ReportId.Value == reportId && ((OwnerDismiss)request).ReviewerUserId.Value == reviewerId && ((OwnerDismiss)request).AdminNote == note
            : request is OwnerReview && ((OwnerReview)request).ReportId.Value == reportId && ((OwnerReview)request).ReviewerUserId.Value == reviewerId && ((OwnerReview)request).AdminNote == note), cancellation.Token);
    }
}
