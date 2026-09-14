using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Contracts.Queries.GetAiPromptRevisions;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Ai.Application.Queries.GetAiPromptRevisions;

public sealed class GetAiPromptRevisionsQueryHandler(IAiPromptTemplateReadModelRepository promptRepository) : IRequestHandler<GetAiPromptRevisionsQuery, IReadOnlyList<AiPromptRevisionReadModel>> {
    public Task<IReadOnlyList<AiPromptRevisionReadModel>> Handle(GetAiPromptRevisionsQuery request, CancellationToken cancellationToken) {
        string key = request.Key;
        string locale = request.Locale;
        return promptRepository.GetRevisionsAsync(key, locale, cancellationToken);
    }

}
