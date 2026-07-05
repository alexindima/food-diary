using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;

namespace FoodDiary.Application.Admin.Queries.GetAdminAiPrompts;

public sealed class GetAdminAiPromptsQueryHandler(IAdminContentReadService adminContentReadService)
    : IQueryHandler<GetAdminAiPromptsQuery, Result<IReadOnlyList<AdminAiPromptModel>>> {
    public async Task<Result<IReadOnlyList<AdminAiPromptModel>>> Handle(
        GetAdminAiPromptsQuery query,
        CancellationToken cancellationToken) {
        IReadOnlyList<AdminAiPromptModel> models = await adminContentReadService.GetAiPromptsAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success(models);
    }
}
