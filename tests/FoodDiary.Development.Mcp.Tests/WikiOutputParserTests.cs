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

    [Fact]
    public void Parse_FallsBackToLineBasedParsingForNonJsonRawOutput() {
        const string output = """
            Raw wiki output without JSON structure.
            - dotnet test AllUnitTests
            Note: dotnet is mentioned here without a leading dash so it is not a check
            Touched FoodDiary.Development.Mcp/Wiki/WikiOutputParser.cs during review
            This is a warning: stale cache detected
            This check failed and needs a re-run
            """;

        WikiCommandResult result = WikiOutputParser.Parse("brief", output, "repository", "head");

        Assert.Null(result.StructuredOutput);
        Assert.Equal(
            ["FoodDiary.Development.Mcp/Wiki/WikiOutputParser.cs"],
            result.ReferencedPaths);
        Assert.Equal(["dotnet test AllUnitTests"], result.RequiredChecks);
        Assert.Equal(
            [
                "This is a warning: stale cache detected",
                "This check failed and needs a re-run",
            ],
            result.Warnings);
    }

    [Fact]
    public void Parse_CollectsNestedArrayAndObjectPathsAndFiltersInvalidCommandValues() {
        const string output = """
            {
              "change": {
                "paths": [
                  ["FoodDiary.Domain/Entities/User.cs"],
                  "not-a-path",
                  null,
                  { "path": "FoodDiary.Application.Users/Handler.cs" },
                  { "other": "FoodDiary.Ignored/Should.cs" }
                ]
              },
              "summaryPath": "FoodDiary.Infrastructure/Repository.cs",
              "count": 42,
              "enabled": true,
              "command": { "command": "dotnet test Alpha", "path": "ignored/for/command" },
              "commands": ["npm run build", "plain text", "wiki.ps1 -Verify"]
            }
            """;

        WikiCommandResult result = WikiOutputParser.Parse("brief", output, "repository", "head");

        Assert.Equal(
            [
                "FoodDiary.Application.Users/Handler.cs",
                "FoodDiary.Domain/Entities/User.cs",
                "FoodDiary.Infrastructure/Repository.cs",
            ],
            result.ReferencedPaths);
        Assert.Equal(
            ["dotnet test Alpha", "npm run build", "wiki.ps1 -Verify"],
            result.RequiredChecks);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Parse_CollectsWarningsFromStringArrayAndObjectMessagesAcrossAllNamedKeys() {
        const string output = """
            {
              "warning": "Single warning message",
              "warnings": ["Array warning one", ""],
              "issues": [{ "message": "Issue from object" }, { "other": "ignored" }],
              "errors": ["Error one", "Error one"]
            }
            """;

        WikiCommandResult result = WikiOutputParser.Parse("brief", output, "repository", "head");

        Assert.Equal(
            [
                "Single warning message",
                "Array warning one",
                "Issue from object",
                "Error one",
            ],
            result.Warnings);
    }

    [Theory]
    [InlineData("not { valid json", false)]
    [InlineData("", false)]
    [InlineData("42", true)]
    public void Parse_TryParseJsonHandlesPrimitiveAndInvalidRawOutput(string rawOutput, bool expectStructuredOutput) {
        WikiCommandResult result = WikiOutputParser.Parse("brief", rawOutput, "repository", "head");

        Assert.Equal(expectStructuredOutput, result.StructuredOutput.HasValue);
    }

    [Fact]
    public void Parse_TraceScopeIgnoresMissingContainersWrongKindsAndUnmatchedCandidatePaths() {
        const string output = """
            {
              "symbols": "not-an-array",
              "candidates": [
                { "declarationPath": "FoodDiary.Application.Users/Declaration.cs" },
                { "path": 123 },
                { "path": "just-plain-text" }
              ]
            }
            """;

        WikiCommandResult result = WikiOutputParser.Parse("trace", output, "repository", "head");

        Assert.Equal(
            ["FoodDiary.Application.Users/Declaration.cs"],
            result.GetScopePaths());
    }
}
