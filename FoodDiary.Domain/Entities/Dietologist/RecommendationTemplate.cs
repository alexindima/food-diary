using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Dietologist;

public sealed class RecommendationTemplate : Entity<RecommendationTemplateId> {
    public UserId DietologistUserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Text { get; private set; } = string.Empty;
    public bool IsArchived { get; private set; }

    private RecommendationTemplate() {
    }

    public static RecommendationTemplate Create(UserId dietologistUserId, string name, string text) {
        if (dietologistUserId == UserId.Empty) {
            throw new ArgumentException("Dietologist id is required.", nameof(dietologistUserId));
        }

        var template = new RecommendationTemplate {
            Id = RecommendationTemplateId.New(),
            DietologistUserId = dietologistUserId,
            Name = Normalize(name, 120, nameof(name)),
            Text = Normalize(text, 2000, nameof(text)),
        };
        template.SetCreated();
        return template;
    }

    public void Update(string name, string text) {
        Name = Normalize(name, 120, nameof(name));
        Text = Normalize(text, 2000, nameof(text));
        SetModified();
    }

    public void Archive() {
        if (IsArchived) {
            return;
        }

        IsArchived = true;
        SetModified();
    }

    private static string Normalize(string value, int maxLength, string paramName) {
        if (string.IsNullOrWhiteSpace(value)) {
            throw new ArgumentException("Value is required.", paramName);
        }

        string normalized = value.Trim();
        return normalized.Length > maxLength
            ? throw new ArgumentOutOfRangeException(paramName, maxLength, "Value is too long.")
            : normalized;
    }
}
