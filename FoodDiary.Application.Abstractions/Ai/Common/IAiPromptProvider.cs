namespace FoodDiary.Application.Abstractions.Ai.Common;

public interface IAiPromptProvider {
    Task<string> GetPromptAsync(string key, CancellationToken cancellationToken = default);
}
