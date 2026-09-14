using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Contracts.Queries.GetAiPromptTemplates;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Ai.Application.Queries.GetAiPromptTemplates;

public sealed class GetAiPromptTemplatesQueryHandler(IAiPromptTemplateReadModelRepository promptRepository) : IRequestHandler<GetAiPromptTemplatesQuery, IReadOnlyList<AiPromptTemplateReadModel>> {
    public Task<IReadOnlyList<AiPromptTemplateReadModel>> Handle(GetAiPromptTemplatesQuery request, CancellationToken cancellationToken) {
        return promptRepository.GetAllReadModelsAsync(cancellationToken);
    }

}
