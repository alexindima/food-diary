namespace FoodDiary.Domain.Entities.Ai;

public sealed class AiPromptRevision {
    public Guid Id { get; private set; }
    public string PromptText { get; private set; } = string.Empty;
    public int Version { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime SavedOnUtc { get; private set; }
    public DateTime ArchivedOnUtc { get; private set; }

    private AiPromptRevision() { }

    internal static AiPromptRevision Capture(AiPromptTemplate template) => new() {
        Id = Guid.NewGuid(),
        PromptText = template.PromptText,
        Version = template.Version,
        IsActive = template.IsActive,
        SavedOnUtc = template.ModifiedOnUtc ?? template.CreatedOnUtc,
        ArchivedOnUtc = DateTime.UtcNow,
    };
}
