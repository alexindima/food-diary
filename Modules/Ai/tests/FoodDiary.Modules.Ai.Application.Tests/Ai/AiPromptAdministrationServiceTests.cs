using FoodDiary.Testing;
using FoodDiary.Modules.Ai.Application.Commands.UpsertAiPrompt;
using FoodDiary.Modules.Ai.Contracts.Commands.UpsertAiPrompt;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Modules.Ai.Domain.Entities;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Ai.Application.Tests.Ai;

[ExcludeFromCodeCoverage]
public sealed class AiPromptAdministrationServiceTests {
    [Fact]
    public async Task UpsertAsync_ReturnsSnapshotThatCannotMutateTheOwnedTemplate() {
        var template = AiPromptTemplate.Create("system", "en", "original", isActive: true);
        IAiPromptTemplateWriteRepository repository = Substitute.For<IAiPromptTemplateWriteRepository>();
        repository.GetByKeyAsync("system", "en", Arg.Any<CancellationToken>()).Returns(template);
        ISender service = RequestTestSender.Create(new UpsertAiPromptCommandHandler(repository));

        AiPromptTemplateReadModel model = ResultAssert.Success(await service.Send(new UpsertAiPromptCommand(Key: "system", Locale: "en", PromptText: "updated", IsActive: true), CancellationToken.None));
        template.Update("later owner edit", isActive: false);

        Assert.Multiple(
            () => Assert.Equal(template.Id.Value, model.Id),
            () => Assert.Equal("updated", model.PromptText),
            () => Assert.True(model.IsActive),
            () => Assert.Equal(2, model.Version));
        await repository.Received(1).GetByKeyAsync("system", "en", Arg.Any<CancellationToken>());
        await repository.Received(1).UpdateAsync(template, Arg.Any<CancellationToken>());
    }
}
