namespace FoodDiary.Development.Mcp.Tests;

[ExcludeFromCodeCoverage]
public sealed class WikiOutputParserTests {
    [Fact]
    public void Parse_UsesStructuredWarningsAndDoesNotTreatJsonPropertyNameAsWarning() {
        const string output = """
            {
              "warnings": [],
              "change": {
                "paths": ["FoodDiary.Development.Mcp/Wiki/WikiQueryService.cs"]
              }
            }
            """;

        WikiCommandResult result = WikiOutputParser.Parse("brief", output, "repository", "head");

        Assert.Empty(result.Warnings);
        Assert.Equal(
            ["FoodDiary.Development.Mcp/Wiki/WikiQueryService.cs"],
            result.ReferencedPaths);
    }

    [Fact]
    public void Parse_SeparatesTraceScopeFromTransitiveContext() {
        const string output = """
            {
              "symbols": [
                { "path": "FoodDiary.Application.Users/Commands/UpdateUser.cs" }
              ],
              "consumers": [
                { "consumerPath": "FoodDiary.Presentation.Api/Users/UpdateUserEndpoint.cs" }
              ],
              "candidates": [
                { "path": "FoodDiary.Application.Users/Queries/GetUser.cs", "confidence": "medium" },
                { "path": "tests/FoodDiary.Application.Tests/NoisyTest.cs", "confidence": "low" }
              ],
              "impact": {
                "references": [
                  { "path": "tests/FoodDiary.ArchitectureTests/UnrelatedTests.cs" }
                ]
              }
            }
            """;

        WikiCommandResult result = WikiOutputParser.Parse("trace", output, "repository", "head");

        Assert.Equal(5, result.ReferencedPaths.Count);
        Assert.Equal(
            [
                "FoodDiary.Application.Users/Commands/UpdateUser.cs",
                "FoodDiary.Application.Users/Queries/GetUser.cs",
                "FoodDiary.Presentation.Api/Users/UpdateUserEndpoint.cs",
            ],
            result.GetScopePaths());
    }
}
