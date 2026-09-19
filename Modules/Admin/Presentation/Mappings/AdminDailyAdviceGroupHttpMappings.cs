using FoodDiary.Modules.Admin.Application.Commands.ImportAdminDailyAdvicePairs;
using FoodDiary.Modules.Admin.Application.Commands.UpdateAdminDailyAdviceGroup;
using FoodDiary.Modules.Admin.Application.Commands.DeleteAdminDailyAdviceGroup;
using FoodDiary.Modules.Admin.Application.Queries.GetAdminDailyAdviceGroups;
using FoodDiary.Modules.Admin.Presentation.Requests;
using FoodDiary.Modules.Admin.Presentation.Responses;
using FoodDiary.Modules.Admin.Application.Models;

namespace FoodDiary.Modules.Admin.Presentation.Mappings;

public static class AdminDailyAdviceGroupHttpMappings {
    public static GetAdminDailyAdviceGroupsQuery ToDailyAdviceGroupsQuery() => new();
    public static DeleteAdminDailyAdviceGroupCommand ToDeleteDailyAdviceGroupCommand(Guid id) => new(id);

    extension(AdminDailyAdviceGroupUpdateHttpRequest request) {
        public UpdateAdminDailyAdviceGroupCommand ToUpdateDailyAdviceGroupCommand(Guid id) =>
            new(id, request.Ru, request.En, request.Weight, request.Tag);
    }

    extension(AdminDailyAdvicePairsImportHttpRequest request) {
        public ImportAdminDailyAdvicePairsCommand ToImportDailyAdvicePairsCommand() => new(request.Version,
            request.Advices?.Select(item => item is null ? null! : new ImportAdminDailyAdvicePairItem(item.Id, item.Ru, item.En, item.Weight, item.Tag)).ToArray()!);
    }

    extension(AdminDailyAdviceGroupModel model) {
        public AdminDailyAdviceGroupHttpResponse ToGroupHttpResponse() => new(model.Id, model.Ru, model.En, model.Weight, model.Tag);
    }

    extension(AdminDailyAdvicesImportModel model) {
        public AdminDailyAdvicesImportHttpResponse ToPairImportHttpResponse() => new(model.ImportedCount, model.SkippedCount);
    }
}
