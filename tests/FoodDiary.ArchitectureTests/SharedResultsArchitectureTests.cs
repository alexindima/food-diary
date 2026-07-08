namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class SharedResultsArchitectureTests {
    [Fact]
    public void ResultsProject_SourceNamespacesMatchProjectName() {
        string root = ArchitectureTestPaths.RepositoryRoot;
        string resultsRoot = ArchitectureTestPaths.FromRoot(Path.Combine("Shared", "FoodDiary.Results"));

        string[] violations = [.. SourceScanner.SourceFiles(resultsRoot)
            .Select(path => new {
                Path = path,
                Namespace = CSharpSyntaxReader.ReadNamespace(path),
            })
            .Where(file => !string.Equals(file.Namespace, "FoodDiary.Results", StringComparison.Ordinal))
            .Select(file => $"{Path.GetRelativePath(root, file.Path)} uses namespace '{file.Namespace ?? "<none>"}'")
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }
}
