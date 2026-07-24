using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Dietologist;

public sealed class RecommendationComment : AggregateRoot<RecommendationCommentId> {
    private const int TextMaxLength = 2000;

    public RecommendationId RecommendationId { get; private set; }
    public UserId AuthorUserId { get; private set; }
    public string Text { get; private set; } = string.Empty;

    public Recommendation Recommendation { get; private set; } = null!;
    public User AuthorUser { get; private set; } = null!;

    private RecommendationComment() {
    }

    public static RecommendationComment Create(
        RecommendationId recommendationId,
        UserId authorUserId,
        string text) {
        if (recommendationId == RecommendationId.Empty) {
            throw new ArgumentException("RecommendationId is required.", nameof(recommendationId));
        }

        if (authorUserId == UserId.Empty) {
            throw new ArgumentException("AuthorUserId is required.", nameof(authorUserId));
        }

        string normalizedText = NormalizeText(text);
        var comment = new RecommendationComment {
            Id = RecommendationCommentId.New(),
            RecommendationId = recommendationId,
            AuthorUserId = authorUserId,
            Text = normalizedText,
        };
        comment.SetCreated();
        return comment;
    }

    private static string NormalizeText(string text) {
        if (string.IsNullOrWhiteSpace(text)) {
            throw new ArgumentException("Comment text is required.", nameof(text));
        }

        string normalized = text.Trim();
        return normalized.Length > TextMaxLength
            ? throw new ArgumentOutOfRangeException(nameof(text), $"Comment text must be at most {TextMaxLength} characters.")
            : normalized;
    }
}
