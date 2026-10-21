using FoodDiary.Modules.Identity.Application.Abstractions.Admin.Common;
using FoodDiary.Modules.Identity.Application.Email.Queries.GetEmailTemplates;
using FoodDiary.Modules.Identity.Contracts.Admin.Models;
using FoodDiary.Modules.Identity.Contracts.Email.Queries.GetEmailTemplates;

namespace FoodDiary.Modules.Identity.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class EmailTemplateQueryContractTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TemplateQuery_ReturnsTheRepositorySnapshotAndForwardsCancellation(bool withTemplates) {
        var created = new DateTime(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);
        IReadOnlyList<EmailTemplateReadModel> expected = withTemplates ? [
            new(Guid.NewGuid(), "welcome", "ru", "Subject", "<p>Welcome</p>", "Welcome", IsActive: true, created, ModifiedOnUtc: null),
            new(Guid.NewGuid(), "welcome", "en", "Draft", "<p>Draft</p>", "Draft", IsActive: false, created, created.AddMinutes(1)),
        ] : [];
        using var cancellation = new CancellationTokenSource();
        IEmailTemplateReadModelRepository repository = Substitute.For<IEmailTemplateReadModelRepository>();
        repository.GetAllReadModelsAsync(cancellation.Token).Returns(expected);

        IReadOnlyList<EmailTemplateReadModel> result = await new GetEmailTemplatesQueryHandler(repository)
            .Handle(new GetEmailTemplatesQuery(), cancellation.Token);

        Assert.Equal(expected, result);
        await repository.Received(1).GetAllReadModelsAsync(cancellation.Token);
        Assert.Single(repository.ReceivedCalls());
    }
}
