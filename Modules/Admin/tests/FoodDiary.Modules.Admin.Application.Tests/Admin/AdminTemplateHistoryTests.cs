using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Application.Abstractions.Email.Common;
using FoodDiary.Application.Admin.Commands.SendAdminEmailTemplateTest;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Admin.Services;
using FoodDiary.Application.ContentReports.Common;
using FoodDiary.Modules.Lessons.Contracts.Common;
using FoodDiary.Results;
using FoodDiary.Application.Ai.Services;
using FoodDiary.Application.Identity.Email.Services;

namespace FoodDiary.Application.Tests.Admin;

[ExcludeFromCodeCoverage]
public sealed class AdminTemplateHistoryTests {
    [Fact]
    public async Task AiUsage_PreservesUserScopeAndCancellation() {
        using var cancellation = new CancellationTokenSource();
        IAiUsageReadRepository repository = Substitute.For<IAiUsageReadRepository>();
        var userId = Guid.NewGuid();
        var summary = new AiUsageSummary(30, 10, 20, [], [], [], []);
        repository.GetSummaryForUserAsync(DateTime.UnixEpoch, DateTime.UnixEpoch.AddDays(1), new FoodDiary.Domain.ValueObjects.Ids.UserId(userId), cancellation.Token).Returns(summary);
        var service = new AiAdministrationReadService(repository, Substitute.For<IAiPromptTemplateReadModelRepository>());
        Assert.Same(summary, await service.GetUsageSummaryForUserAsync(DateTime.UnixEpoch, DateTime.UnixEpoch.AddDays(1), userId, cancellation.Token));
    }

    [Fact]
    public async Task EmailRevisions_PreserveArchivedContentWithoutAiVersion() {
        using var cancellation = new CancellationTokenSource();
        IEmailTemplateReadModelRepository emailRepository = Substitute.For<IEmailTemplateReadModelRepository>();
        IEmailTemplateAdministrationReadService email = new EmailTemplateAdministrationReadService(emailRepository);
        IAiPromptTemplateReadModelRepository aiRepository = Substitute.For<IAiPromptTemplateReadModelRepository>();
        IAiAdministrationReadService ai = new AiAdministrationReadService(Substitute.For<IAiUsageReadRepository>(), aiRepository);
        var revision = new EmailTemplateRevisionReadModel(Guid.NewGuid(), "subject", "html", "text", IsActive: false, DateTime.UnixEpoch, DateTime.UnixEpoch.AddDays(1));
        emailRepository.GetRevisionsAsync("welcome", "en", cancellation.Token).Returns([revision]);
        var service = new AdminContentReadService(Substitute.For<ILessonAdministrationReadService>(), email, ai, Substitute.For<IContentReportAdministrationReadService>());
        AdminTemplateRevisionModel actual = Assert.Single(await service.GetTemplateRevisionsAsync("welcome", "en", isAiPrompt: false, cancellation.Token));
        Assert.Equal(new AdminTemplateRevisionModel(revision.Id, "subject", "html", "text", IsActive: false, Version: null, revision.SavedOnUtc, revision.ArchivedOnUtc), actual);
        Assert.Empty(aiRepository.ReceivedCalls());
    }

    [Fact]
    public async Task AiRevisions_PreserveVersionAndPromptWithoutEmailFields() {
        using var cancellation = new CancellationTokenSource();
        IEmailTemplateReadModelRepository emailRepository = Substitute.For<IEmailTemplateReadModelRepository>();
        IEmailTemplateAdministrationReadService email = new EmailTemplateAdministrationReadService(emailRepository);
        IAiPromptTemplateReadModelRepository aiRepository = Substitute.For<IAiPromptTemplateReadModelRepository>();
        IAiAdministrationReadService ai = new AiAdministrationReadService(Substitute.For<IAiUsageReadRepository>(), aiRepository);
        var revision = new AiPromptRevisionReadModel(Guid.NewGuid(), "prompt", 3, IsActive: true, DateTime.UnixEpoch, DateTime.UnixEpoch.AddDays(1));
        aiRepository.GetRevisionsAsync("welcome", "ru", cancellation.Token).Returns([revision]);
        var service = new AdminContentReadService(Substitute.For<ILessonAdministrationReadService>(), email, ai, Substitute.For<IContentReportAdministrationReadService>());
        AdminTemplateRevisionModel actual = Assert.Single(await service.GetTemplateRevisionsAsync("welcome", "ru", isAiPrompt: true, cancellation.Token));
        Assert.Equal(new AdminTemplateRevisionModel(revision.Id, Subject: null, HtmlBody: null, "prompt", IsActive: true, 3, revision.SavedOnUtc, revision.ArchivedOnUtc), actual);
        Assert.Empty(emailRepository.ReceivedCalls());
    }

    [Theory]
    [InlineData("account_created", "https://fooddiary.club/login")]
    [InlineData("password_reset", "https://fooddiary.club/reset-password?userId=demo&token=demo")]
    [InlineData("dietologist_invitation", "https://fooddiary.club/dietologist-invitations/demo")]
    [InlineData("verify_email", "https://fooddiary.club/verify-email?userId=demo&token=demo")]
    public async Task TestEmail_UsesCorrectSampleLink(string key, string link) {
        using var cancellation = new CancellationTokenSource();
        IEmailTransport transport = Substitute.For<IEmailTransport>();
        var handler = new SendAdminEmailTemplateTestCommandHandler(new EmailOptions { FromAddress = "from@example.com", FromName = "FoodDiary" }, transport);
        Result result = await handler.Handle(new SendAdminEmailTemplateTestCommand("to@example.com", key, "{{link}}", "{{link}}", "{{link}}"), cancellation.Token);
        ResultAssert.Success(result);
        await transport.Received(1).SendAsync(Arg.Is<EmailMessage>(message => message.Subject == link && message.HtmlBody == link && message.TextBody == link), cancellation.Token);
    }
}
