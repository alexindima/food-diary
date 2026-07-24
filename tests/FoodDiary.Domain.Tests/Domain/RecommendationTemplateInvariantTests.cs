using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class RecommendationTemplateInvariantTests {
    [Fact]
    public void Create_NormalizesValuesAndStartsActive() {
        var template = RecommendationTemplate.Create(
            UserId.New(),
            "  Breakfast  ",
            "  Add protein  ");

        Assert.Multiple(
            () => Assert.Equal("Breakfast", template.Name),
            () => Assert.Equal("Add protein", template.Text),
            () => Assert.False(template.IsArchived));
    }

    [Fact]
    public void Update_ReplacesNormalizedValues() {
        var template = RecommendationTemplate.Create(UserId.New(), "Old", "Old text");

        template.Update("  New  ", "  New text  ");

        Assert.Multiple(
            () => Assert.Equal("New", template.Name),
            () => Assert.Equal("New text", template.Text));
    }

    [Fact]
    public void Archive_IsIdempotent() {
        var template = RecommendationTemplate.Create(UserId.New(), "Name", "Text");

        template.Archive();
        template.Archive();

        Assert.True(template.IsArchived);
    }

    [Theory]
    [InlineData("", "Text")]
    [InlineData("Name", " ")]
    public void Create_WhenRequiredValueIsBlank_Throws(string name, string text) {
        Assert.Throws<ArgumentException>(() => RecommendationTemplate.Create(UserId.New(), name, text));
    }
}
