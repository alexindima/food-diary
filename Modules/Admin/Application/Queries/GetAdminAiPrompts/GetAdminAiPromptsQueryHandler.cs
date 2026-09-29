using FoodDiary.Mediator;
using FoodDiary.Modules.Ai.Contracts.Queries.GetAiPromptTemplates;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Admin.Application.Mappings;
using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Results;
using FoodDiary.Application.Contracts.Common.Validation;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminAiPrompts;

public sealed class GetAdminAiPromptsQueryHandler(ISender aiReadService)
    : IQueryHandler<GetAdminAiPromptsQuery, Result<IReadOnlyList<AdminAiPromptModel>>> {
    public async Task<Result<IReadOnlyList<AdminAiPromptModel>>> Handle(GetAdminAiPromptsQuery query, CancellationToken cancellationToken) {
        int page = PaginationPolicy.NormalizePage(query.Page);
        int limit = PaginationPolicy.NormalizePageSize(query.Limit);
        IReadOnlyList<AiPromptTemplateReadModel> templates = await aiReadService.Send(new GetAiPromptTemplatesQuery(page, limit), cancellationToken)
            .ConfigureAwait(false);
        return Result.Success<IReadOnlyList<AdminAiPromptModel>>(templates.Select(static template => template.ToAdminModel()).ToList());
    }
}
