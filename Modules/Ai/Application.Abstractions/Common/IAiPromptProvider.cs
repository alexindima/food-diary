namespace FoodDiary.Modules.Ai.Application.Abstractions.Common;

public interface IAiPromptProvider {
    Task<string> GetPromptAsync(string key, CancellationToken cancellationToken = default);
}
