using FoodDiary.Domain.Entities.Content;

namespace FoodDiary.Application.Tests.Domain;

public class EmailTemplateInvariantTests
{
    [Fact]
    public void Create_NormalizesKeyLocaleAndFields()
    {
        var template = EmailTemplate.Create(
            key: "  EMAIL_VERIFICATION  ",
            locale: "ru-RU",
            subject: "  Confirm email  ",
            htmlBody: "  <p>Hello</p>  ",
            textBody: "  Hello  ",
            isActive: true);

        Assert.Equal("email_verification", template.Key);
        Assert.Equal("ru", template.Locale);
        Assert.Equal("Confirm email", template.Subject);
        Assert.Equal("<p>Hello</p>", template.HtmlBody);
        Assert.Equal("Hello", template.TextBody);
    }

    [Fact]
    public void Create_WithBlankKey_Throws()
    {
        Assert.Throws<ArgumentException>(() => EmailTemplate.Create(
            key: "   ",
            locale: "en",
            subject: "Subject",
            htmlBody: "<p>Body</p>",
            textBody: "Body",
            isActive: true));
    }

    [Fact]
    public void Create_WithTooLongKey_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => EmailTemplate.Create(
            key: new string('k', 65),
            locale: "en",
            subject: "Subject",
            htmlBody: "<p>Body</p>",
            textBody: "Body",
            isActive: true));
    }

    [Fact]
    public void Create_WithBlankSubject_Throws()
    {
        Assert.Throws<ArgumentException>(() => EmailTemplate.Create(
            key: "email_verification",
            locale: "en",
            subject: "  ",
            htmlBody: "<p>Body</p>",
            textBody: "Body",
            isActive: true));
    }

    [Fact]
    public void Create_WithTooLongSubject_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => EmailTemplate.Create(
            key: "email_verification",
            locale: "en",
            subject: new string('s', 257),
            htmlBody: "<p>Body</p>",
            textBody: "Body",
            isActive: true));
    }

    [Fact]
    public void Create_WithBlankBodies_Throws()
    {
        Assert.Throws<ArgumentException>(() => EmailTemplate.Create(
            key: "email_verification",
            locale: "en",
            subject: "Subject",
            htmlBody: " ",
            textBody: "Body",
            isActive: true));

        Assert.Throws<ArgumentException>(() => EmailTemplate.Create(
            key: "email_verification",
            locale: "en",
            subject: "Subject",
            htmlBody: "<p>Body</p>",
            textBody: " ",
            isActive: true));
    }

    [Fact]
    public void Update_WithSameNormalizedValues_DoesNotSetModifiedOnUtc()
    {
        var template = EmailTemplate.Create(
            key: "email_verification",
            locale: "en",
            subject: "Subject",
            htmlBody: "<p>Body</p>",
            textBody: "Body",
            isActive: true);

        template.Update(
            subject: "  Subject  ",
            htmlBody: "  <p>Body</p>  ",
            textBody: "  Body  ",
            isActive: true);

        Assert.Null(template.ModifiedOnUtc);
    }

    [Fact]
    public void Update_WithChanges_SetsModifiedAndAppliesValues()
    {
        var template = EmailTemplate.Create(
            key: "email_verification",
            locale: "en",
            subject: "Subject",
            htmlBody: "<p>Body</p>",
            textBody: "Body",
            isActive: true);

        template.Update(
            subject: " New Subject ",
            htmlBody: " <p>New Body</p> ",
            textBody: " New Body ",
            isActive: false);

        Assert.Equal("New Subject", template.Subject);
        Assert.Equal("<p>New Body</p>", template.HtmlBody);
        Assert.Equal("New Body", template.TextBody);
        Assert.False(template.IsActive);
        Assert.NotNull(template.ModifiedOnUtc);
    }
}
