using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Admin.Mappings;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Queries.GetAdminAiPrompts;

public sealed class GetAdminAiPromptsQueryHandler(IAiAdministrationReadService aiReadService)
    : IQueryHandler<GetAdminAiPromptsQuery, Result<IReadOnlyList<AdminAiPromptModel>>> {
    public async Task<Result<IReadOnlyList<AdminAiPromptModel>>> Handle(GetAdminAiPromptsQuery query, CancellationToken cancellationToken) {
        IReadOnlyList<AiPromptTemplateReadModel> templates = await aiReadService
            .GetPromptTemplatesAsync(cancellationToken)
            .ConfigureAwait(false);
        return Result.Success<IReadOnlyList<AdminAiPromptModel>>(templates.Select(static template => template.ToAdminModel()).ToList());
    }
}
