namespace FoodDiary.Domain.Entities.Content;

public sealed class EmailTemplateRevision {
    public Guid Id { get; private set; }
    public string Subject { get; private set; } = string.Empty;
    public string HtmlBody { get; private set; } = string.Empty;
    public string TextBody { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTime SavedOnUtc { get; private set; }
    public DateTime ArchivedOnUtc { get; private set; }

    private EmailTemplateRevision() { }

    internal static EmailTemplateRevision Capture(EmailTemplate template) => new() {
        Id = Guid.NewGuid(),
        Subject = template.Subject,
        HtmlBody = template.HtmlBody,
        TextBody = template.TextBody,
        IsActive = template.IsActive,
        SavedOnUtc = template.ModifiedOnUtc ?? template.CreatedOnUtc,
        ArchivedOnUtc = DateTime.UtcNow,
    };
}
