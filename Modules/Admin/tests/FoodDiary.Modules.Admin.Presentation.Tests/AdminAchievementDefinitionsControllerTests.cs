using FoodDiary.Application.Admin.Commands.CreateAdminAchievementDefinition;
using FoodDiary.Application.Admin.Commands.UpdateAdminAchievementDefinition;
using FoodDiary.Application.Admin.Queries.GetAdminAchievementDefinitions;
using FoodDiary.Application.Gamification.Models;
using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Presentation.Api.Features.Admin;
using FoodDiary.Presentation.Api.Features.Admin.Requests;
using FoodDiary.Presentation.Api.Features.Admin.Responses;
using FoodDiary.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class AdminAchievementDefinitionsControllerTests {
    [Fact]
    public async Task Endpoints_MapCreateUpdateAndListContracts() {
        AchievementDefinitionAdminModel model = CreateModel();
        CapturedSender createSender = SubstituteSender.Capture(Result.Success(model));
        AdminAchievementDefinitionsController createController = CreateController(createSender);

        IActionResult createResult = await createController.Create(new CreateAdminAchievementDefinitionHttpRequest(
            model.Key, model.Category, model.Metric, model.Threshold, model.TitleRu, model.TitleEn,
            model.DescriptionRu, model.DescriptionEn, model.Icon, model.SortOrder, model.IsActive));

        Assert.IsType<AdminAchievementDefinitionHttpResponse>(Assert.IsType<CreatedResult>(createResult).Value);
        CreateAdminAchievementDefinitionCommand createCommand = Assert.IsType<CreateAdminAchievementDefinitionCommand>(createSender.Request);
        Assert.Equal(model.Key, createCommand.Input.Key);

        CapturedSender updateSender = SubstituteSender.Capture(Result.Success(model with { Version = 2 }));
        AdminAchievementDefinitionsController updateController = CreateController(updateSender);
        IActionResult updateResult = await updateController.Update(model.Id, new UpdateAdminAchievementDefinitionHttpRequest(
            model.Category, model.Metric, model.Threshold, model.TitleRu, model.TitleEn,
            model.DescriptionRu, model.DescriptionEn, model.Icon, model.SortOrder, model.IsActive, model.Version));

        Assert.IsType<OkObjectResult>(updateResult);
        UpdateAdminAchievementDefinitionCommand updateCommand = Assert.IsType<UpdateAdminAchievementDefinitionCommand>(updateSender.Request);
        Assert.Multiple(
            () => Assert.Equal(model.Id, updateCommand.Id),
            () => Assert.Equal(model.Version, updateCommand.Input.Version));

        CapturedSender listSender = SubstituteSender.Capture(Result.Success<IReadOnlyList<AchievementDefinitionAdminModel>>([model]));
        IActionResult listResult = await CreateController(listSender).GetAll();
        Assert.IsType<List<AdminAchievementDefinitionHttpResponse>>(Assert.IsType<OkObjectResult>(listResult).Value);
        Assert.IsType<GetAdminAchievementDefinitionsQuery>(listSender.Request);
    }

    [Fact]
    public void Controller_RequiresAdminRole() {
        AuthorizeAttribute authorize = Assert.Single(typeof(AdminAchievementDefinitionsController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).Cast<AuthorizeAttribute>());

        Assert.Equal(PresentationRoleNames.Admin, authorize.Roles);
    }

    private static AdminAchievementDefinitionsController CreateController(CapturedSender sender) => new(sender) {
        ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
    };

    private static AchievementDefinitionAdminModel CreateModel() => new(
        Guid.NewGuid(), "meals_20", "meals", "TotalMeals", 20,
        "20 приёмов", "20 meals", "Описание", "Description", "restaurant", SortOrder: 20, IsActive: true, Version: 1);
}
