using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Mediator;
using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Modules.Admin.Application.Mappings;
using FoodDiary.Modules.Ai.Contracts.Queries.GetAiPromptScenarios;
using FoodDiary.Results;
namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminAiPromptScenarios;

public sealed class GetAdminAiPromptScenariosQueryHandler(ISender sender)
    : IQueryHandler<GetAdminAiPromptScenariosQuery, Result<IReadOnlyList<AdminAiPromptScenarioModel>>> {
    public async Task<Result<IReadOnlyList<AdminAiPromptScenarioModel>>> Handle(GetAdminAiPromptScenariosQuery request, CancellationToken cancellationToken) {
        IReadOnlyList<FoodDiary.Modules.Ai.Contracts.Models.AiPromptScenarioModel> scenarios = await sender.Send(new GetAiPromptScenariosQuery(), cancellationToken).ConfigureAwait(false);
        return Result.Success<IReadOnlyList<AdminAiPromptScenarioModel>>(scenarios.Select(item => new AdminAiPromptScenarioModel(
            item.Key, item.Locale, item.PromptText, item.Source, item.SourceLocale, item.InheritedPromptText,
            item.InheritedSource, item.Template?.ToAdminModel(), item.Variables, item.ResponseFormatJson)).ToArray());
    }
}
