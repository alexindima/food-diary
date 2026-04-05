using FoodDiary.Domain.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Ai;

public sealed class AiPromptTemplate : Entity<AiPromptTemplateId> {
    private const int KeyMaxLength = 64;
    private const int LocaleMaxLength = 8;
    private const int PromptTextMaxLength = 4096;

    public string Key { get; private set; } = string.Empty;
    public string Locale { get; private set; } = string.Empty;
    public string PromptText { get; private set; } = string.Empty;
    public int Version { get; private set; } = 1;
    public bool IsActive { get; private set; } = true;

    private AiPromptTemplate() {
    }

    public static AiPromptTemplate Create(
        string key,
        string locale,
        string promptText,
        bool isActive = true) {
        var template = new AiPromptTemplate {
            Id = AiPromptTemplateId.New(),
            Key = NormalizeRequired(key, KeyMaxLength, nameof(key)).ToLowerInvariant(),
            Locale = NormalizeRequired(locale, LocaleMaxLength, nameof(locale)).ToLowerInvariant(),
            PromptText = NormalizeRequired(promptText, PromptTextMaxLength, nameof(promptText)),
            Version = 1,
            IsActive = isActive,
        };
        template.SetCreated();
        return template;
    }

    public void Update(string promptText, bool? isActive = null) {
        var normalizedText = NormalizeRequired(promptText, PromptTextMaxLength, nameof(promptText));
        var changed = false;

        if (PromptText != normalizedText) {
            PromptText = normalizedText;
            Version++;
            changed = true;
        }

        if (isActive.HasValue && IsActive != isActive.Value) {
            IsActive = isActive.Value;
            changed = true;
        }

        if (changed) {
            SetModified();
        }
    }

    private static string NormalizeRequired(string value, int maxLength, string paramName) {
        if (string.IsNullOrWhiteSpace(value)) {
            throw new ArgumentException($"{paramName} is required.", paramName);
        }

        var trimmed = value.Trim();
        return trimmed.Length > maxLength
            ? throw new ArgumentOutOfRangeException(paramName, $"Must be at most {maxLength} characters.")
            : trimmed;
    }
}
