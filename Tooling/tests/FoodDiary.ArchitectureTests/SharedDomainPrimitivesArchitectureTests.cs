namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class SharedDomainPrimitivesArchitectureTests {
    [Fact]
    public void DomainPrimitivesProject_SourceNamespacesMatchProjectName() {
        string root = ArchitectureTestPaths.RepositoryRoot;
        string domainPrimitivesRoot = ArchitectureTestPaths.FromRoot(Path.Combine("Shared", "FoodDiary.Domain.Primitives"));

        string[] violations = [.. SourceScanner.SourceFiles(domainPrimitivesRoot)
            .Select(path => new {
                Path = path,
                Namespace = CSharpSyntaxReader.ReadNamespace(path),
            })
            .Where(file => !string.Equals(file.Namespace, "FoodDiary.Domain.Primitives", StringComparison.Ordinal))
            .Select(file => $"{Path.GetRelativePath(root, file.Path)} uses namespace '{file.Namespace ?? "<none>"}'")
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }
}
