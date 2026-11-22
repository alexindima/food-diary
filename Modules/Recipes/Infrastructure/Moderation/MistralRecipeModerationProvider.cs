using FoodDiary.Modules.Recipes.Application.Abstractions.Moderation;

namespace FoodDiary.Modules.Recipes.Infrastructure.Moderation;

internal sealed class MistralRecipeModerationProvider(HttpClient client, string? apiKey) : IRecipeModerationProvider {
    private const string Model = "mistral-moderation-2603";
    private static readonly Uri Endpoint = new("https://api.mistral.ai/v1/moderations");

    public string Name => "Mistral";
    public bool IsAvailable => !string.IsNullOrWhiteSpace(apiKey);

    public Task<RecipeModerationCheck> CheckAsync(RecipeModerationContent content, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(apiKey)) {
            throw new RecipeModerationProviderException("Mistral moderation is not configured.", retryable: false);
        }

        if (string.IsNullOrWhiteSpace(content.Text) || content.Images.Count > 0) {
            throw new RecipeModerationProviderException("Mistral moderation requires text only.", retryable: false);
        }

        return RecipeModerationHttp.SendAsync(
            client, Endpoint, apiKey, Model, content.Text, "text", requiredCategories: null, cancellationToken);
    }
}
