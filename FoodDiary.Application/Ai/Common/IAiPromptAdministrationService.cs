using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Results;

namespace FoodDiary.Application.Ai.Common;

public interface IAiPromptAdministrationService {
    Task<Result<AiPromptTemplate>> UpsertAsync(
        string key,
        string locale,
        string promptText,
        bool isActive,
        CancellationToken cancellationToken);
}
