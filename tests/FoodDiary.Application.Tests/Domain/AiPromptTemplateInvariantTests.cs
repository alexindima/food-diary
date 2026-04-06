using FoodDiary.Domain.Entities.Ai;

namespace FoodDiary.Application.Tests.Domain;

public class AiPromptTemplateInvariantTests {
    [Fact]
    public void Create_WithBlankKey_Throws() {
        Assert.Throws<ArgumentException>(() =>
            AiPromptTemplate.Create("   ", "en", "Prompt text"));
    }

    [Fact]
    public void Create_WithBlankLocale_Throws() {
        Assert.Throws<ArgumentException>(() =>
            AiPromptTemplate.Create("meal_scan", "   ", "Prompt text"));
    }

    [Fact]
    public void Create_WithBlankPromptText_Throws() {
        Assert.Throws<ArgumentException>(() =>
            AiPromptTemplate.Create("meal_scan", "en", "   "));
    }

    [Fact]
    public void Create_WithTooLongKey_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            AiPromptTemplate.Create(new string('k', 65), "en", "Prompt text"));
    }

    [Fact]
    public void Create_WithTooLongLocale_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            AiPromptTemplate.Create("meal_scan", new string('l', 9), "Prompt text"));
    }

    [Fact]
    public void Create_WithTooLongPromptText_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            AiPromptTemplate.Create("meal_scan", "en", new string('p', 4097)));
    }

    [Fact]
    public void Create_NormalizesKeyAndLocaleToLowerTrimmed() {
        var template = AiPromptTemplate.Create("  MEAL_SCAN  ", "  EN  ", "  Prompt text  ");

        Assert.Equal("meal_scan", template.Key);
        Assert.Equal("en", template.Locale);
        Assert.Equal("Prompt text", template.PromptText);
    }

    [Fact]
    public void Create_SetsVersionTo1AndIsActiveToTrue() {
        var template = AiPromptTemplate.Create("key", "en", "text");

        Assert.Equal(1, template.Version);
        Assert.True(template.IsActive);
    }

    [Fact]
    public void Create_WithIsActiveFalse_SetsIsActive() {
        var template = AiPromptTemplate.Create("key", "en", "text", isActive: false);

        Assert.False(template.IsActive);
    }

    [Fact]
    public void Update_WithNewPromptText_IncrementsVersion() {
        var template = AiPromptTemplate.Create("key", "en", "old text");

        template.Update("new text");

        Assert.Equal("new text", template.PromptText);
        Assert.Equal(2, template.Version);
        Assert.NotNull(template.ModifiedOnUtc);
    }

    [Fact]
    public void Update_WithSamePromptText_DoesNotIncrementVersion() {
        var template = AiPromptTemplate.Create("key", "en", "same text");

        template.Update("same text");

        Assert.Equal(1, template.Version);
        Assert.Null(template.ModifiedOnUtc);
    }

    [Fact]
    public void Update_WithIsActiveChange_SetsModifiedOnUtc() {
        var template = AiPromptTemplate.Create("key", "en", "text");

        template.Update("text", isActive: false);

        Assert.False(template.IsActive);
        Assert.NotNull(template.ModifiedOnUtc);
        Assert.Equal(1, template.Version);
    }

    [Fact]
    public void Update_WithBlankPromptText_Throws() {
        var template = AiPromptTemplate.Create("key", "en", "text");

        Assert.Throws<ArgumentException>(() => template.Update("   "));
    }
}
