namespace FoodDiary.Modules.Recipes.Application.Abstractions.Moderation;

public sealed class RecipeModerationProviderException(
    string message, bool retryable, TimeSpan? retryAfter = null, bool requiresContentChanges = false) : Exception(message) {
    public bool Retryable { get; } = retryable;
    public TimeSpan? RetryAfter { get; } = retryAfter;
    public bool RequiresContentChanges { get; } = requiresContentChanges;
}
