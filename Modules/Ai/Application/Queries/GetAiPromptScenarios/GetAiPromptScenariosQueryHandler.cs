using FoodDiary.Mediator;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Application.Abstractions.Prompts;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Modules.Ai.Contracts.Queries.GetAiPromptScenarios;

namespace FoodDiary.Modules.Ai.Application.Queries.GetAiPromptScenarios;

public sealed class GetAiPromptScenariosQueryHandler(IAiPromptTemplateReadModelRepository repository, IAiPromptPreviewRenderer renderer)
    : IRequestHandler<GetAiPromptScenariosQuery, IReadOnlyList<AiPromptScenarioModel>> {
    public async Task<IReadOnlyList<AiPromptScenarioModel>> Handle(GetAiPromptScenariosQuery request, CancellationToken cancellationToken) {
        IReadOnlyList<AiPromptTemplateReadModel> templates = await repository.GetAllReadModelsAsync(cancellationToken).ConfigureAwait(false);
        return AiPromptCatalog.Keys.SelectMany(key => new[] { "en", "ru" }.Select(locale => {
            AiPromptTemplateReadModel? template = templates.FirstOrDefault(item => string.Equals(item.Key, key, StringComparison.Ordinal) && string.Equals(item.Locale, locale, StringComparison.Ordinal));
            AiPromptTemplateReadModel? english = string.Equals(locale, "en", StringComparison.Ordinal) ? null
                : templates.FirstOrDefault(item => string.Equals(item.Key, key, StringComparison.Ordinal) && string.Equals(item.Locale, "en", StringComparison.Ordinal) && item.IsActive);
            string inherited = english?.PromptText ?? AiPromptCatalog.GetDefault(key)!;
            string inheritedSource = english is null ? "built-in" : "english";
            bool custom = template?.IsActive == true;
            return new AiPromptScenarioModel(key, locale, custom ? template!.PromptText : inherited,
                custom ? "custom" : inheritedSource, custom ? locale : "en", inherited, inheritedSource,
                template, AiPromptCatalog.GetVariables(key), renderer.GetResponseFormatJson(key));
        })).ToArray();
    }
}
