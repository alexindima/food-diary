using FoodDiary.Modules.Identity.Application.Abstractions.Admin.Common;
using FoodDiary.Testing;
using FoodDiary.Modules.Identity.Application.Email.Queries.GetEmailTemplateRevisions;
using FoodDiary.Modules.Identity.Application.Email.Queries.GetEmailTemplates;
using FoodDiary.Mediator;
using FoodDiary.Modules.Ai.Application.Queries.GetAiPromptRevisions;
using FoodDiary.Modules.Ai.Application.Queries.GetAiPromptTemplates;
using FoodDiary.Modules.Ai.Application.Queries.GetAiUsageForUser;
using FoodDiary.Modules.Ai.Application.Queries.GetAiUsageSummary;
using FoodDiary.Modules.Ai.Contracts.Queries.GetAiUsageForUser;
using FoodDiary.Modules.Admin.Application.Queries.GetAdminTemplateRevisions;
using FoodDiary.Modules.Identity.Contracts.Admin.Models;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Application.Abstractions.Email.Common;
using FoodDiary.Modules.Admin.Application.Commands.SendAdminEmailTemplateTest;
using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Tests.Admin;

[ExcludeFromCodeCoverage]
public sealed class AdminTemplateHistoryTests {
    [Fact]
    public async Task AiUsage_PreservesUserScopeAndCancellation() {
        using var cancellation = new CancellationTokenSource();
        IAiUsageQuery repository = Substitute.For<IAiUsageQuery>();
        var userId = Guid.NewGuid();
        var summary = new AiUsageSummary(30, 10, 20, [], [], [], []);
        repository.GetSummaryForUserAsync(DateTime.UnixEpoch, DateTime.UnixEpoch.AddDays(1), new FoodDiary.Domain.ValueObjects.Ids.UserId(userId), cancellation.Token).Returns(summary);
        ISender service = RequestTestSender.Create(new GetAiPromptRevisionsQueryHandler(Substitute.For<IAiPromptTemplateReadModelRepository>()), new GetAiUsageForUserQueryHandler(repository), new GetAiUsageSummaryQueryHandler(repository), new GetAiPromptTemplatesQueryHandler(Substitute.For<IAiPromptTemplateReadModelRepository>()));
        Assert.Same(summary, await service.Send(new GetAiUsageForUserQuery(FromUtc: DateTime.UnixEpoch, ToUtc: DateTime.UnixEpoch.AddDays(1), UserId: userId), cancellation.Token));
    }

    [Fact]
    public async Task EmailRevisions_PreserveArchivedContentWithoutAiVersion() {
        using var cancellation = new CancellationTokenSource();
        IEmailTemplateReadModelRepository emailRepository = Substitute.For<IEmailTemplateReadModelRepository>();
        ISender email = RequestTestSender.Create(new GetEmailTemplateRevisionsQueryHandler(emailRepository), new GetEmailTemplatesQueryHandler(emailRepository));
        IAiPromptTemplateReadModelRepository aiRepository = Substitute.For<IAiPromptTemplateReadModelRepository>();
        ISender ai = RequestTestSender.Create(new GetAiPromptRevisionsQueryHandler(aiRepository), new GetAiUsageForUserQueryHandler(Substitute.For<IAiUsageQuery>()), new GetAiUsageSummaryQueryHandler(Substitute.For<IAiUsageQuery>()), new GetAiPromptTemplatesQueryHandler(aiRepository));
        var revision = new EmailTemplateRevisionReadModel(Guid.NewGuid(), "subject", "html", "text", IsActive: false, DateTime.UnixEpoch, DateTime.UnixEpoch.AddDays(1));
        emailRepository.GetRevisionsAsync("welcome", "en", cancellation.Token).Returns([revision]);
        var service = new GetAdminTemplateRevisionsQueryHandler(RequestTestSender.Route((email, [typeof(global::FoodDiary.Modules.Identity.Contracts.Email.Queries.GetEmailTemplateRevisions.GetEmailTemplateRevisionsQuery)]), (ai, [typeof(global::FoodDiary.Modules.Ai.Contracts.Queries.GetAiPromptRevisions.GetAiPromptRevisionsQuery)])));
        AdminTemplateRevisionModel actual = Assert.Single((await service.Handle(new GetAdminTemplateRevisionsQuery("welcome", "en", IsAiPrompt: false), cancellation.Token)).Value);
        Assert.Equal(new AdminTemplateRevisionModel(revision.Id, "subject", "html", "text", IsActive: false, Version: null, revision.SavedOnUtc, revision.ArchivedOnUtc), actual);
        Assert.Empty(aiRepository.ReceivedCalls());
    }

    [Fact]
    public async Task AiRevisions_PreserveVersionAndPromptWithoutEmailFields() {
        using var cancellation = new CancellationTokenSource();
        IEmailTemplateReadModelRepository emailRepository = Substitute.For<IEmailTemplateReadModelRepository>();
        ISender email = RequestTestSender.Create(new GetEmailTemplateRevisionsQueryHandler(emailRepository), new GetEmailTemplatesQueryHandler(emailRepository));
        IAiPromptTemplateReadModelRepository aiRepository = Substitute.For<IAiPromptTemplateReadModelRepository>();
        ISender ai = RequestTestSender.Create(new GetAiPromptRevisionsQueryHandler(aiRepository), new GetAiUsageForUserQueryHandler(Substitute.For<IAiUsageQuery>()), new GetAiUsageSummaryQueryHandler(Substitute.For<IAiUsageQuery>()), new GetAiPromptTemplatesQueryHandler(aiRepository));
        var revision = new AiPromptRevisionReadModel(Guid.NewGuid(), "prompt", 3, IsActive: true, DateTime.UnixEpoch, DateTime.UnixEpoch.AddDays(1));
        aiRepository.GetRevisionsAsync("welcome", "ru", cancellation.Token).Returns([revision]);
        var service = new GetAdminTemplateRevisionsQueryHandler(RequestTestSender.Route((email, [typeof(global::FoodDiary.Modules.Identity.Contracts.Email.Queries.GetEmailTemplateRevisions.GetEmailTemplateRevisionsQuery)]), (ai, [typeof(global::FoodDiary.Modules.Ai.Contracts.Queries.GetAiPromptRevisions.GetAiPromptRevisionsQuery)])));
        AdminTemplateRevisionModel actual = Assert.Single((await service.Handle(new GetAdminTemplateRevisionsQuery("welcome", "ru", IsAiPrompt: true), cancellation.Token)).Value);
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
