using FoodDiary.Application.Admin.Mappings;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Domain.Entities.Ai;

namespace FoodDiary.Application.Admin.Queries.GetAdminAiPrompts;

public class GetAdminAiPromptsQueryHandler(IAiPromptTemplateReadRepository repository)
    : IQueryHandler<GetAdminAiPromptsQuery, Result<IReadOnlyList<AdminAiPromptModel>>> {
    public async Task<Result<IReadOnlyList<AdminAiPromptModel>>> Handle(
        GetAdminAiPromptsQuery query,
        CancellationToken cancellationToken) {
        IReadOnlyList<AiPromptTemplate> templates = await repository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        var models = templates.Select(static template => template.ToAdminModel()).ToList();
        return Result.Success<IReadOnlyList<AdminAiPromptModel>>(models);
    }
}
