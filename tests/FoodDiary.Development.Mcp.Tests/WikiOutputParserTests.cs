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
              "impact": {
                "references": [
                  { "path": "tests/FoodDiary.ArchitectureTests/UnrelatedTests.cs" }
                ]
              }
            }
            """;

        WikiCommandResult result = WikiOutputParser.Parse("trace", output, "repository", "head");

        Assert.Equal(3, result.ReferencedPaths.Count);
        Assert.Equal(
            [
                "FoodDiary.Application.Users/Commands/UpdateUser.cs",
                "FoodDiary.Presentation.Api/Users/UpdateUserEndpoint.cs",
            ],
            result.GetScopePaths());
    }
}
