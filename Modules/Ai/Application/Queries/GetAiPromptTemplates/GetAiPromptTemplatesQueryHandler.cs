using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Contracts.Queries.GetAiPromptTemplates;
using FoodDiary.Mediator;
using FoodDiary.Application.Contracts.Common.Validation;

namespace FoodDiary.Modules.Ai.Application.Queries.GetAiPromptTemplates;

public sealed class GetAiPromptTemplatesQueryHandler(IAiPromptTemplateReadModelRepository promptRepository) : IRequestHandler<GetAiPromptTemplatesQuery, IReadOnlyList<AiPromptTemplateReadModel>> {
    public Task<IReadOnlyList<AiPromptTemplateReadModel>> Handle(GetAiPromptTemplatesQuery request, CancellationToken cancellationToken) {
        return promptRepository.GetPageReadModelsAsync(
            PaginationPolicy.NormalizePage(request.Page),
            PaginationPolicy.NormalizePageSize(request.Limit),
            cancellationToken);
    }

}
