namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class RepositoryFileDiscoveryTests {
    [Fact]
    public void Discovery_PreservesOwnedSourcesAndUnlistedProjectsWithoutBuildCopies() {
        DirectoryInfo root = Directory.CreateTempSubdirectory("fooddiary-discovery-");
        try {
            string[] included = ["Modules/New/Application/New.csproj", "Modules/New/tests/New.Tests.csproj",
                "Shared/Contracts/Contracts.csproj", ".llm-wiki/tools/Wiki.csproj", "Modules/Binary/Valid.csproj"];
            string[] excluded = [".artifacts/worktree/Copy.csproj", "node_modules/package/Copy.csproj",
                ".git/Copy.csproj", ".angular/cache/Copy.csproj", "Modules/New/BIN/Copy.csproj", "Shared/obj/Copy.csproj"];
            foreach (string relative in included.Concat(excluded)) {
                string path = Path.Combine(root.FullName, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, "<Project />");
            }

            string[] actual = [.. RepositoryFileDiscovery.EnumerateFiles(root.FullName, "*.csproj")
                .Select(path => Path.GetRelativePath(root.FullName, path).Replace('\\', '/')).Order(StringComparer.Ordinal)];
            Assert.Equal(included.Order(StringComparer.Ordinal), actual, StringComparer.Ordinal);

            string source = Path.GetFullPath(Path.Combine(root.FullName, "Modules/New/Application/Handler.cs"));
            File.WriteAllText(source, "class Handler { }");
            File.WriteAllText(Path.ChangeExtension(source, ".g.cs"), "class Generated { }");
            Assert.Equal([source], SourceScanner.SourceFiles(root.FullName), StringComparer.Ordinal);
        } finally {
            root.Delete(recursive: true);
        }
    }
}
