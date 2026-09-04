namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class RetiredErrorFacadeTests {
    [Theory]
    [InlineData("DailyAdvices", "DailyAdvice")]
    [InlineData("Fasting", "Fasting")]
    [InlineData("Hydration", "HydrationEntry")]
    public void CentralAbstractions_DoNotDeclareOrExportRetiredFacade(string module, string facade) {
        string central = ArchitectureTestPaths.FromRoot("FoodDiary.Application.Abstractions");
        string[] declarations = [.. SourceScanner.SourceFiles(central)
            .SelectMany(CSharpSyntaxReader.ReadTypeDeclarations)
            .Where(type => string.Equals(type.Name, facade, StringComparison.Ordinal))
            .Select(type => type.Path)];

        Assert.Multiple(
            () => Assert.Empty(declarations),
            () => Assert.False(File.Exists(Path.Combine(central, "Common", "Abstractions", "Results", $"Errors.{facade}.cs"))),
            () => Assert.DoesNotContain($"FoodDiary.Modules.{module}.Application.Abstractions",
                ProjectReferenceReader.ReadProjectReferences("FoodDiary.Application.Abstractions/FoodDiary.Application.Abstractions.csproj"),
                StringComparer.Ordinal));
    }
}
