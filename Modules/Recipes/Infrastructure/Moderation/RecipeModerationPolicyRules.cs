using System.Text.Json;
using FoodDiary.Modules.Recipes.Contracts.Moderation;

namespace FoodDiary.Modules.Recipes.Infrastructure.Moderation;

internal static class RecipeModerationPolicyRules {
    private static readonly string[] ImageCategories = [
        "self-harm", "self-harm/intent", "self-harm/instructions", "sexual", "violence", "violence/graphic",
    ];
    private static readonly string[] TextCategories = [
        .. ImageCategories, "harassment", "harassment/threatening", "hate", "hate/threatening", "illicit", "illicit/violent", "sexual/minors",
    ];

    public static readonly Guid DefaultVersionId = new("c0a17afe-0737-4fc4-9cf7-4ce3090be7a6");

    public static RecipeModerationPolicyVersion Default { get; } =
        new(DefaultVersionId, DateTimeOffset.UnixEpoch, PublishedBy: null, new(TextCategories, ImageCategories, MistralSamplePercent: 10));

    public static RecipeModerationPolicyInput Validate(RecipeModerationPolicyInput input) {
        ArgumentNullException.ThrowIfNull(input);
        if (input.MistralSamplePercent is < 0 or > 100) {
            throw new ArgumentException("The Mistral sample must be between 0 and 100.", nameof(input));
        }

        return new RecipeModerationPolicyInput(
            ValidateCategories(input.TextReviewCategories, TextCategories),
            ValidateCategories(input.ImageReviewCategories, ImageCategories),
            input.MistralSamplePercent);
    }

    public static RecipeModerationPolicyInput Deserialize(string json) =>
        JsonSerializer.Deserialize<RecipeModerationPolicyInput>(json)
        ?? throw new InvalidOperationException("A moderation policy version is missing its rules.");

    private static IReadOnlyList<string> ValidateCategories(IReadOnlyList<string>? categories, string[] supported) {
        if (categories is null || categories.Count == 0 || categories.Any(category => !Array.Exists(supported, value => string.Equals(value, category, StringComparison.Ordinal)))) {
            throw new ArgumentException("Select supported moderation categories for both text and images.", nameof(categories));
        }

        return [.. categories.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];
    }
}
