using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Cycles.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Cycles.Domain.Entities;

public sealed class CycleDayNote : Entity<CycleDayNoteId> {
    public CycleProfileId CycleProfileId { get; private set; }
    public DateOnly Date { get; private set; }
    public string Notes { get; private set; } = string.Empty;
    public CycleProfile CycleProfile { get; private set; } = null!;

    private CycleDayNote() {
    }

    private CycleDayNote(CycleDayNoteId id) : base(id) {
    }

    public static CycleDayNote Create(CycleProfileId cycleProfileId, DateOnly date, string notes) {
        if (cycleProfileId == CycleProfileId.Empty) {
            throw new ArgumentException("CycleProfileId is required.", nameof(cycleProfileId));
        }

        var note = new CycleDayNote(CycleDayNoteId.New()) {
            CycleProfileId = cycleProfileId,
            Date = date,
            Notes = NormalizeNotes(notes),
        };
        note.SetCreated();
        return note;
    }

    public void Update(string notes) {
        Notes = NormalizeNotes(notes);
        SetModified();
    }

    private static string NormalizeNotes(string notes) =>
        CycleProfile.NormalizeNotes(notes) ?? throw new ArgumentException("Notes are required.", nameof(notes));
}
