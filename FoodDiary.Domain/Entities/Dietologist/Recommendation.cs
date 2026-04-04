using FoodDiary.Domain.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Events;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Dietologist;

public sealed class Recommendation : AggregateRoot<RecommendationId> {
    private const int TextMaxLength = 2000;

    public UserId DietologistUserId { get; private set; }
    public UserId ClientUserId { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public bool IsRead { get; private set; }
    public DateTime? ReadAtUtc { get; private set; }

    public User DietologistUser { get; private set; } = null!;
    public User ClientUser { get; private set; } = null!;

    private Recommendation() {
    }

    public static Recommendation Create(
        UserId dietologistUserId,
        UserId clientUserId,
        string text) {
        EnsureUserId(dietologistUserId, nameof(dietologistUserId));
        EnsureUserId(clientUserId, nameof(clientUserId));

        var normalizedText = NormalizeText(text);
        var recommendation = new Recommendation {
            Id = RecommendationId.New(),
            DietologistUserId = dietologistUserId,
            ClientUserId = clientUserId,
            Text = normalizedText,
            IsRead = false,
        };
        recommendation.SetCreated();
        recommendation.RaiseDomainEvent(new RecommendationCreatedDomainEvent(
            recommendation.Id, dietologistUserId, clientUserId));
        return recommendation;
    }

    public void MarkAsRead() {
        if (IsRead) {
            return;
        }

        IsRead = true;
        ReadAtUtc = DomainTime.UtcNow;
        SetModified();
    }

    private static string NormalizeText(string value) {
        if (string.IsNullOrWhiteSpace(value)) {
            throw new ArgumentException("Recommendation text is required.", nameof(value));
        }

        var normalized = value.Trim();
        return normalized.Length > TextMaxLength
            ? throw new ArgumentOutOfRangeException(nameof(value), $"Recommendation text must be at most {TextMaxLength} characters.")
            : normalized;
    }

    private static void EnsureUserId(UserId userId, string paramName) {
        if (userId == UserId.Empty) {
            throw new ArgumentException("UserId is required.", paramName);
        }
    }
}
