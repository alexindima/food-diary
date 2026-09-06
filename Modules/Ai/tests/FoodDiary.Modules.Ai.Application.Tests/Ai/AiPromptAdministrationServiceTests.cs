using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Application.Ai.Services;
using FoodDiary.Domain.Entities.Ai;

namespace FoodDiary.Application.Tests.Ai;

[ExcludeFromCodeCoverage]
public sealed class AiPromptAdministrationServiceTests {
    [Fact]
    public async Task UpsertAsync_ReturnsSnapshotThatCannotMutateTheOwnedTemplate() {
        var template = AiPromptTemplate.Create("system", "en", "original", isActive: true);
        IAiPromptTemplateWriteRepository repository = Substitute.For<IAiPromptTemplateWriteRepository>();
        repository.GetByKeyAsync("system", "en", Arg.Any<CancellationToken>()).Returns(template);
        repository.GetByIdAsync(template.Id, asTracking: true, Arg.Any<CancellationToken>()).Returns(template);
        var service = new AiPromptAdministrationService(repository);

        AiPromptTemplateReadModel model = ResultAssert.Success(await service.UpsertAsync("system", "en", "updated", isActive: true, CancellationToken.None));
        template.Update("later owner edit", isActive: false);

        Assert.Multiple(
            () => Assert.Equal(template.Id.Value, model.Id),
            () => Assert.Equal("updated", model.PromptText),
            () => Assert.True(model.IsActive),
            () => Assert.Equal(2, model.Version));
        await repository.Received(1).UpdateAsync(template, Arg.Any<CancellationToken>());
    }
}
