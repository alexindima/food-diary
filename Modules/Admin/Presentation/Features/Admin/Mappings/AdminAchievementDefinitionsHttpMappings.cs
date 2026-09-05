using FoodDiary.Application.Admin.Commands.CreateAdminAchievementDefinition;
using FoodDiary.Application.Admin.Commands.UpdateAdminAchievementDefinition;
using FoodDiary.Application.Admin.Queries.GetAdminAchievementDefinitions;
using FoodDiary.Application.Gamification.Models;
using FoodDiary.Presentation.Api.Features.Admin.Requests;
using FoodDiary.Presentation.Api.Features.Admin.Responses;

namespace FoodDiary.Presentation.Api.Features.Admin.Mappings;

public static class AdminAchievementDefinitionsHttpMappings {
    public static GetAdminAchievementDefinitionsQuery ToQuery() => new();

    extension(CreateAdminAchievementDefinitionHttpRequest request) {
        public CreateAdminAchievementDefinitionCommand ToCommand() =>
            new(new AchievementDefinitionCreateInput(
                request.Key, request.Category, request.Metric, request.Threshold, request.TitleRu, request.TitleEn,
                request.DescriptionRu, request.DescriptionEn, request.Icon, request.SortOrder, request.IsActive));
    }

    extension(UpdateAdminAchievementDefinitionHttpRequest request) {
        public UpdateAdminAchievementDefinitionCommand ToCommand(Guid id) =>
            new(id, new AchievementDefinitionUpdateInput(
                request.Category, request.Metric, request.Threshold, request.TitleRu, request.TitleEn,
                request.DescriptionRu, request.DescriptionEn, request.Icon, request.SortOrder, request.IsActive,
                request.Version));
    }

    extension(AchievementDefinitionAdminModel model) {
        public AdminAchievementDefinitionHttpResponse ToHttpResponse() =>
            new(model.Id, model.Key, model.Category, model.Metric, model.Threshold, model.TitleRu, model.TitleEn,
                model.DescriptionRu, model.DescriptionEn, model.Icon, model.SortOrder, model.IsActive, model.Version);
    }
}
