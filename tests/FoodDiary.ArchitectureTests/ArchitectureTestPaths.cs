namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
internal static class ArchitectureTestPaths {
    public static string RepositoryRoot { get; } = FindRepositoryRoot();

    public static string FromRoot(params string[] pathParts) =>
        Path.Combine([RepositoryRoot, .. pathParts]);

    public static bool IsGeneratedOrBuildPath(string path) =>
        path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
        path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
        path.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase) ||
        path.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) ||
        path.EndsWith(".AssemblyInfo.cs", StringComparison.OrdinalIgnoreCase);

    private static string FindRepositoryRoot() {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null) {
            string solutionPath = Path.Combine(current.FullName, "FoodDiary.slnx");
            if (File.Exists(solutionPath)) {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Repository root was not found.");
    }
}
