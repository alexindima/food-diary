using FoodDiary.Modules.Export.Application.Services;

namespace FoodDiary.Modules.Export.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class CsvFieldEscaperTests {
    [Theory]
    [InlineData("  =1+1", "'  =1+1")]
    [InlineData("\t@command", "'\t@command")]
    [InlineData("\u0001+1", "'\u0001+1")]
    [InlineData("  ordinary text", "  ordinary text")]
    [InlineData(" \t ", " \t ")]
    public void Escape_ExaminesFirstMeaningfulCharacterWithoutDiscardingWhitespace(string input, string expected) {
        Assert.Equal(expected, CsvFieldEscaper.Escape(input));
    }
}
