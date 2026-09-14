using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Ai.Contracts.Common;

public interface IAiPromptAdministrationService {
    Task<Result<AiPromptTemplateReadModel>> UpsertAsync(
        string key,
        string locale,
        string promptText,
        bool isActive,
        CancellationToken cancellationToken);
}
