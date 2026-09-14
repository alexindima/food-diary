using FoodDiary.Modules.Ai.Contracts.Common;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Admin.Application.Mappings;
using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminAiPrompts;

public sealed class GetAdminAiPromptsQueryHandler(IAiAdministrationReadService aiReadService)
    : IQueryHandler<GetAdminAiPromptsQuery, Result<IReadOnlyList<AdminAiPromptModel>>> {
    public async Task<Result<IReadOnlyList<AdminAiPromptModel>>> Handle(GetAdminAiPromptsQuery query, CancellationToken cancellationToken) {
        IReadOnlyList<AiPromptTemplateReadModel> templates = await aiReadService
            .GetPromptTemplatesAsync(cancellationToken)
            .ConfigureAwait(false);
        return Result.Success<IReadOnlyList<AdminAiPromptModel>>(templates.Select(static template => template.ToAdminModel()).ToList());
    }
}
