using FoodDiary.Mediator;
using FoodDiary.Modules.Gamification.Contracts.Commands.CreateAchievementDefinition;
using FoodDiary.Modules.Gamification.Contracts.Commands.UpdateAchievementDefinition;
using FoodDiary.Modules.Gamification.Contracts.Queries.GetAchievementDefinitionsForAdministration;
using FoodDiary.Modules.Admin.Application.Commands.CreateAdminAchievementDefinition;
using FoodDiary.Modules.Admin.Application.Commands.UpdateAdminAchievementDefinition;
using FoodDiary.Modules.Admin.Application.Queries.GetAdminAchievementDefinitions;
using FoodDiary.Modules.Gamification.Contracts.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Tests.Admin;

[ExcludeFromCodeCoverage]
public sealed class AdminAchievementDefinitionHandlerTests {
    [Fact]
    public async Task Handlers_DelegateToAdministrationService() {
        ISender service = Substitute.For<ISender>();
        var model = new AchievementDefinitionAdminModel(
            Id: Guid.NewGuid(), Key: "key", Category: "category", Metric: "metric", Threshold: 1,
            TitleRu: "ru", TitleEn: "en", DescriptionRu: "ru", DescriptionEn: "en", Icon: "icon",
            SortOrder: 1, IsActive: true, Version: 1);
        var createInput = new AchievementDefinitionCreateInput(
            Key: "key", Category: "category", Metric: "metric", Threshold: 1,
            TitleRu: "ru", TitleEn: "en", DescriptionRu: "ru", DescriptionEn: "en", Icon: "icon",
            SortOrder: 1, IsActive: true);
        var updateInput = new AchievementDefinitionUpdateInput(
            Category: "category", Metric: "metric", Threshold: 2,
            TitleRu: "ru2", TitleEn: "en2", DescriptionRu: "ru2", DescriptionEn: "en2", Icon: "icon2",
            SortOrder: 2, IsActive: false, Version: 1);
        service.Send(new CreateAchievementDefinitionCommand(Input: createInput), Arg.Any<CancellationToken>()).Returns(Result.Success(model));
        service.Send(new UpdateAchievementDefinitionCommand(Id: model.Id, Input: updateInput), Arg.Any<CancellationToken>()).Returns(Result.Success(model));
        service.Send(new GetAchievementDefinitionsForAdministrationQuery(), Arg.Any<CancellationToken>()).Returns([model]);

        AchievementDefinitionAdminModel created = ResultAssert.Success(await new CreateAdminAchievementDefinitionCommandHandler(service)
            .Handle(new CreateAdminAchievementDefinitionCommand(createInput), CancellationToken.None));
        AchievementDefinitionAdminModel updated = ResultAssert.Success(await new UpdateAdminAchievementDefinitionCommandHandler(service)
            .Handle(new UpdateAdminAchievementDefinitionCommand(model.Id, updateInput), CancellationToken.None));
        IReadOnlyList<AchievementDefinitionAdminModel> all = ResultAssert.Success(
            await new GetAdminAchievementDefinitionsQueryHandler(service)
                .Handle(new GetAdminAchievementDefinitionsQuery(), CancellationToken.None));

        Assert.Multiple(
            () => Assert.Same(model, created),
            () => Assert.Same(model, updated),
            () => Assert.Same(model, Assert.Single(all)));
    }
}
