using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.Entities;

public sealed class EmailTemplate : Entity<Guid>
{
    public string Key { get; private set; } = string.Empty;
    public string Locale { get; private set; } = "en";
    public string Subject { get; private set; } = string.Empty;
    public string HtmlBody { get; private set; } = string.Empty;
    public string TextBody { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    private EmailTemplate()
    {
    }

    public static EmailTemplate Create(
        string key,
        string locale,
        string subject,
        string htmlBody,
        string textBody,
        bool isActive)
    {
        var template = new EmailTemplate
        {
            Id = Guid.NewGuid(),
            Key = key,
            Locale = locale,
            Subject = subject,
            HtmlBody = htmlBody,
            TextBody = textBody,
            IsActive = isActive
        };
        template.SetCreated();
        return template;
    }

    public void Update(
        string subject,
        string htmlBody,
        string textBody,
        bool isActive)
    {
        Subject = subject;
        HtmlBody = htmlBody;
        TextBody = textBody;
        IsActive = isActive;
        SetModified();
    }
}
