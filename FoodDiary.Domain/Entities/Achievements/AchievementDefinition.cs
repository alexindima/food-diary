using FoodDiary.Domain.Enums;
using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Achievements;

public sealed class AchievementDefinition : Entity<AchievementDefinitionId> {
    public const int KeyMaxLength = 100;
    public const int CategoryMaxLength = 50;
    public const int TitleMaxLength = 160;
    public const int DescriptionMaxLength = 500;
    public const int IconMaxLength = 50;

    public string Key { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public AchievementMetric Metric { get; private set; }
    public int Threshold { get; private set; }
    public string TitleRu { get; private set; } = string.Empty;
    public string TitleEn { get; private set; } = string.Empty;
    public string DescriptionRu { get; private set; } = string.Empty;
    public string DescriptionEn { get; private set; } = string.Empty;
    public string Icon { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }
    public int Version { get; private set; }

    private AchievementDefinition() {
    }

    public static AchievementDefinition Create(
        string key,
        string category,
        AchievementMetric metric,
        int threshold,
        string titleRu,
        string titleEn,
        string descriptionRu,
        string descriptionEn,
        string icon,
        int sortOrder,
        bool isActive = true) {
        var definition = new AchievementDefinition {
            Id = AchievementDefinitionId.New(),
            Key = NormalizeCode(key, KeyMaxLength, nameof(key)),
            Version = 1,
        };
        definition.Apply(category, metric, threshold, titleRu, titleEn, descriptionRu, descriptionEn, icon, sortOrder, isActive);
        definition.SetCreated();
        return definition;
    }

    public void Update(
        string category,
        AchievementMetric metric,
        int threshold,
        string titleRu,
        string titleEn,
        string descriptionRu,
        string descriptionEn,
        string icon,
        int sortOrder,
        bool isActive) {
        int nextVersion = GetNextVersion();
        Apply(category, metric, threshold, titleRu, titleEn, descriptionRu, descriptionEn, icon, sortOrder, isActive);
        Version = nextVersion;
        SetModified();
    }

    private int GetNextVersion() {
        return Version == int.MaxValue
            ? throw new InvalidOperationException("Achievement definition version limit has been reached.")
            : Version + 1;
    }

    private void Apply(
        string category,
        AchievementMetric metric,
        int threshold,
        string titleRu,
        string titleEn,
        string descriptionRu,
        string descriptionEn,
        string icon,
        int sortOrder,
        bool isActive) {
        if (!Enum.IsDefined(metric)) {
            throw new ArgumentOutOfRangeException(nameof(metric));
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(threshold);
        ArgumentOutOfRangeException.ThrowIfNegative(sortOrder);

        string normalizedCategory = NormalizeCode(category, CategoryMaxLength, nameof(category));
        string normalizedTitleRu = NormalizeRequired(titleRu, TitleMaxLength, nameof(titleRu));
        string normalizedTitleEn = NormalizeRequired(titleEn, TitleMaxLength, nameof(titleEn));
        string normalizedDescriptionRu = NormalizeRequired(descriptionRu, DescriptionMaxLength, nameof(descriptionRu));
        string normalizedDescriptionEn = NormalizeRequired(descriptionEn, DescriptionMaxLength, nameof(descriptionEn));
        string normalizedIcon = NormalizeCode(icon, IconMaxLength, nameof(icon));

        Category = normalizedCategory;
        Metric = metric;
        Threshold = threshold;
        TitleRu = normalizedTitleRu;
        TitleEn = normalizedTitleEn;
        DescriptionRu = normalizedDescriptionRu;
        DescriptionEn = normalizedDescriptionEn;
        Icon = normalizedIcon;
        SortOrder = sortOrder;
        IsActive = isActive;
    }

    private static string NormalizeCode(string value, int maxLength, string paramName) {
        string normalized = NormalizeRequired(value, maxLength, paramName).ToLowerInvariant();
        return normalized.All(character => char.IsAsciiLetterOrDigit(character) || character is '_' or '-')
            ? normalized
            : throw new ArgumentException("Only ASCII letters, digits, underscores and hyphens are allowed.", paramName);
    }

    private static string NormalizeRequired(string value, int maxLength, string paramName) {
        if (string.IsNullOrWhiteSpace(value)) {
            throw new ArgumentException("Value is required.", paramName);
        }

        string normalized = value.Trim();
        return normalized.Length <= maxLength
            ? normalized
            : throw new ArgumentOutOfRangeException(paramName);
    }
}
