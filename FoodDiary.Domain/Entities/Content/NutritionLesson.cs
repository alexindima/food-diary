using FoodDiary.Domain.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Content;

public sealed class NutritionLesson : Entity<NutritionLessonId> {
    private const int TitleMaxLength = 256;
    private const int ContentMaxLength = 8192;
    private const int SummaryMaxLength = 512;
    private const int LocaleMaxLength = 10;

    public string Title { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public string? Summary { get; private set; }
    public string Locale { get; private set; } = string.Empty;
    public LessonCategory Category { get; private set; }
    public LessonDifficulty Difficulty { get; private set; }
    public int EstimatedReadMinutes { get; private set; }
    public int SortOrder { get; private set; }

    private NutritionLesson() {
    }

    public static NutritionLesson Create(
        string title,
        string content,
        string? summary,
        string locale,
        LessonCategory category,
        LessonDifficulty difficulty,
        int estimatedReadMinutes,
        int sortOrder = 0) {
        var lesson = new NutritionLesson {
            Id = NutritionLessonId.New(),
            Title = NormalizeRequired(title, TitleMaxLength, nameof(title)),
            Content = NormalizeRequired(content, ContentMaxLength, nameof(content)),
            Summary = NormalizeOptional(summary, SummaryMaxLength),
            Locale = NormalizeRequired(locale, LocaleMaxLength, nameof(locale)).ToLowerInvariant(),
            Category = category,
            Difficulty = difficulty,
            EstimatedReadMinutes = Math.Max(1, estimatedReadMinutes),
            SortOrder = Math.Max(0, sortOrder),
        };
        lesson.SetCreated();
        return lesson;
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

    private static string? NormalizeOptional(string? value, int maxLength) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }
}
