using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Abstractions.Ai.Common;

public interface IAiPromptAdministrationService {
    Task<Result<AiPromptTemplateReadModel>> UpsertAsync(
        string key,
        string locale,
        string promptText,
        bool isActive,
        CancellationToken cancellationToken);
}
