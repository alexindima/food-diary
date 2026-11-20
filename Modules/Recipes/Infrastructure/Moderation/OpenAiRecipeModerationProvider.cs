using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Images.Service.Contracts.Common;
using FoodDiary.Modules.Recipes.Application.Abstractions.Moderation;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Recipes.Infrastructure.Moderation;

internal sealed class OpenAiRecipeModerationProvider(
    HttpClient client, IImageAssetContentService images, string? apiKey) : IRecipeModerationProvider {
    private const string Model = "omni-moderation-latest";
    private static readonly Uri Endpoint = new("https://api.openai.com/v1/moderations");
    private static readonly string[] ImageCategories = [
        "self-harm", "self-harm/intent", "self-harm/instructions", "sexual", "violence", "violence/graphic",
    ];
    private static readonly string[] TextCategories = [
        .. ImageCategories, "harassment", "harassment/threatening", "hate", "hate/threatening", "illicit", "illicit/violent", "sexual/minors",
    ];

    public string Name => "OpenAi";
    public bool IsAvailable => !string.IsNullOrWhiteSpace(apiKey);

    public async Task<RecipeModerationCheck> CheckAsync(RecipeModerationContent content, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(apiKey)) {
            throw new RecipeModerationProviderException("OpenAI moderation is not configured.", retryable: false);
        }

        if (content.Images.Any(static image => image.AssetId is null || image.AssetId == Guid.Empty)
            || (content.Images.Count > 0 && content.OwnerId == Guid.Empty)) {
            throw new RecipeModerationProviderException(
                "Moderation requires confirmed, owned image assets.", retryable: false, requiresContentChanges: true);
        }

        string? checkedModel = null;
        var signals = new List<RecipeModerationSignal>(content.Images.Count + 1);
        if (!string.IsNullOrWhiteSpace(content.Text)) {
            AddCheck(await RecipeModerationHttp.SendAsync(
                client, Endpoint, apiKey, Model, content.Text, "text", TextCategories, cancellationToken));
        }

        foreach (RecipeModerationImage image in content.Images) {
            string dataUrl = await ResolveImageAsync(image, (UserId)content.OwnerId, cancellationToken);
            var input = new[] { new { type = "image_url", image_url = new { url = dataUrl } } };
            AddCheck(await RecipeModerationHttp.SendAsync(
                client, Endpoint, apiKey, Model, input, image.Field, ImageCategories, cancellationToken));
        }

        if (checkedModel is null) {
            throw new RecipeModerationProviderException("Moderation requires text or images.", retryable: false);
        }

        return new RecipeModerationCheck(checkedModel, signals);

        void AddCheck(RecipeModerationCheck check) {
            if (checkedModel is not null && !string.Equals(check.Model, checkedModel, StringComparison.Ordinal)) {
                throw new RecipeModerationProviderException("OpenAI moderation returned different model versions for one check.", retryable: true);
            }

            checkedModel = check.Model;
            signals.AddRange(check.Signals);
        }
    }

    private async Task<string> ResolveImageAsync(RecipeModerationImage image, UserId userId, CancellationToken cancellationToken) {
        var assetId = (ImageAssetId)image.AssetId.GetValueOrDefault();
        Result<string> result = await images.GetDataUrlAsync(assetId, userId, cancellationToken);
        if (result.IsFailure) {
            bool retryable = result.Error.Kind == ErrorKind.ExternalFailure;
            throw new RecipeModerationProviderException(
                "Moderation could not read a confirmed recipe image.",
                retryable,
                requiresContentChanges: !retryable);
        }

        return result.Value;
    }
}
