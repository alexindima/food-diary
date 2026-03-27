using FoodDiary.Domain.Common;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Meals;

public sealed class MealAiSession : Entity<MealAiSessionId> {
    private const int NotesMaxLength = 2048;

    public MealId MealId { get; private set; }
    public ImageAssetId? ImageAssetId { get; private set; }
    public DateTime RecognizedAtUtc { get; private set; }
    public string? Notes { get; private set; }

    public Meal Meal { get; private set; } = null!;
    public ImageAsset? ImageAsset { get; private set; }
    private readonly List<MealAiItem> _items = [];
    public IReadOnlyCollection<MealAiItem> Items => _items.AsReadOnly();

    private MealAiSession() {
    }

    internal static MealAiSession Create(
        MealId mealId,
        ImageAssetId? imageAssetId,
        DateTime recognizedAtUtc,
        string? notes) {
        EnsureMealId(mealId);

        var session = new MealAiSession {
            Id = MealAiSessionId.New(),
            MealId = mealId,
            ImageAssetId = imageAssetId,
            RecognizedAtUtc = NormalizeUtc(recognizedAtUtc),
            Notes = NormalizeOptionalText(notes, NotesMaxLength, nameof(notes))
        };
        session.SetCreated();
        return session;
    }

    internal void AddItems(IReadOnlyList<MealAiItem> items) {
        ArgumentNullException.ThrowIfNull(items);

        if (items.Count == 0) {
            return;
        }

        foreach (var item in items) {
            if (item is null) {
                throw new ArgumentException("AI session item cannot be null.", nameof(items));
            }

            if (item.MealAiSessionId != Id) {
                throw new ArgumentException("AI item must be attached to this session before adding.", nameof(items));
            }

            _items.Add(item);
        }

        SetModified();
    }

    private static DateTime NormalizeUtc(DateTime value) {
        if (value.Kind == DateTimeKind.Unspecified) {
            throw new ArgumentOutOfRangeException(nameof(value), "UTC timestamp kind must be specified.");
        }

        return value.ToUniversalTime();
    }

    private static string? NormalizeOptionalText(string? value, int maxLength, string paramName) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length > maxLength
            ? throw new ArgumentOutOfRangeException(paramName, $"Value must be at most {maxLength} characters.")
            : normalized;
    }

    private static void EnsureMealId(MealId mealId) {
        if (mealId == MealId.Empty) {
            throw new ArgumentException("MealId is required.", nameof(mealId));
        }
    }
}
