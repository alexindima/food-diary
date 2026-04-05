using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Ai.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;

namespace FoodDiary.Application.Admin.Queries.GetAdminAiPrompts;

public class GetAdminAiPromptsQueryHandler(IAiPromptTemplateRepository repository)
    : IQueryHandler<GetAdminAiPromptsQuery, Result<IReadOnlyList<AdminAiPromptModel>>> {
    public async Task<Result<IReadOnlyList<AdminAiPromptModel>>> Handle(
        GetAdminAiPromptsQuery query,
        CancellationToken cancellationToken) {
        var templates = await repository.GetAllAsync(cancellationToken);
        var models = templates
            .Select(t => new AdminAiPromptModel(
                t.Id.Value, t.Key, t.Locale, t.PromptText,
                t.Version, t.IsActive, t.CreatedOnUtc, t.ModifiedOnUtc))
            .ToList();
        return Result.Success<IReadOnlyList<AdminAiPromptModel>>(models);
    }
}
