using System.Text.Json;
using FoodDiary.Domain.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Tracking;

public sealed class CycleSymptomEntry : Entity<CycleSymptomEntryId> {
    public CycleProfileId CycleProfileId { get; private set; }
    public DateTime Date { get; private set; }
    public CycleSymptomCategory Category { get; private set; }
    public int Intensity { get; private set; }
    public string TagsJson { get; private set; } = "[]";
    public string? Note { get; private set; }

    public CycleProfile CycleProfile { get; private set; } = null!;

    public IReadOnlyList<string> Tags => JsonSerializer.Deserialize<IReadOnlyList<string>>(TagsJson) ?? [];

    private CycleSymptomEntry() {
    }

    private CycleSymptomEntry(CycleSymptomEntryId id) : base(id) {
    }

    public static CycleSymptomEntry Create(
        CycleProfileId cycleProfileId,
        DateTime date,
        CycleSymptomCategory category,
        int intensity,
        IReadOnlyCollection<string> tags,
        string? note) {
        EnsureCycleProfileId(cycleProfileId);
        EnsureDefined(category, nameof(category));

        var entry = new CycleSymptomEntry(CycleSymptomEntryId.New()) {
            CycleProfileId = cycleProfileId,
            Date = CycleProfile.NormalizeDate(date),
            Category = category,
            Intensity = CycleProfile.NormalizeIntensity(intensity, nameof(intensity)),
            TagsJson = NormalizeTags(tags),
            Note = CycleProfile.NormalizeNotes(note),
        };

        entry.SetCreated();
        return entry;
    }

    public void Update(int intensity, IReadOnlyCollection<string> tags, string? note, bool clearNote) {
        Intensity = CycleProfile.NormalizeIntensity(intensity, nameof(intensity));
        TagsJson = NormalizeTags(tags);
        if (clearNote) {
            Note = null;
        } else if (note is not null) {
            Note = CycleProfile.NormalizeNotes(note);
        }
        SetModified();
    }

    private static string NormalizeTags(IReadOnlyCollection<string> tags) {
        ArgumentNullException.ThrowIfNull(tags);
        string[] normalized = [
            .. tags
            .Select(tag => tag.Trim())
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase),
        ];

        return JsonSerializer.Serialize(normalized);
    }

    private static void EnsureCycleProfileId(CycleProfileId cycleProfileId) {
        if (cycleProfileId == CycleProfileId.Empty) {
            throw new ArgumentException("CycleProfileId is required.", nameof(cycleProfileId));
        }
    }

    private static void EnsureDefined<TEnum>(TEnum value, string paramName)
        where TEnum : struct, Enum {
        if (!Enum.IsDefined(value)) {
            throw new ArgumentOutOfRangeException(paramName, "Value must be one of the supported values.");
        }
    }
}
