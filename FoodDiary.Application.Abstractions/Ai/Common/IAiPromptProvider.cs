namespace FoodDiary.Application.Ai.Common;

public interface IAiPromptProvider {
    Task<string> GetPromptAsync(string key, CancellationToken cancellationToken = default);
}
