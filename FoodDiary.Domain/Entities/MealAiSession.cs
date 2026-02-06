using System;
using FoodDiary.Domain.Common;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Entities;

/// <summary>
/// AI recognition session for a meal.
/// </summary>
public sealed class MealAiSession : Entity<MealAiSessionId>
{
    public MealId MealId { get; private set; }
    public ImageAssetId? ImageAssetId { get; private set; }
    public DateTime RecognizedAtUtc { get; private set; }
    public string? Notes { get; private set; }

    // Navigation properties
    public Meal Meal { get; private set; } = null!;
    private readonly List<MealAiItem> _items = new();
    public IReadOnlyCollection<MealAiItem> Items => _items.AsReadOnly();

    private MealAiSession() { }

    internal static MealAiSession Create(
        MealId mealId,
        ImageAssetId? imageAssetId,
        DateTime recognizedAtUtc,
        string? notes)
    {
        var session = new MealAiSession
        {
            Id = MealAiSessionId.New(),
            MealId = mealId,
            ImageAssetId = imageAssetId,
            RecognizedAtUtc = recognizedAtUtc,
            Notes = notes
        };
        session.SetCreated();
        return session;
    }

    internal void AddItems(IReadOnlyList<MealAiItem> items)
    {
        if (items.Count == 0)
        {
            return;
        }

        foreach (var item in items)
        {
            _items.Add(item);
        }

        SetModified();
    }
}
