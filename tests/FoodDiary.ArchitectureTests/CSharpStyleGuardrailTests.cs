namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class CSharpStyleGuardrailTests {
    [Fact]
    public void RedundantGlobalSystemQualification_IsNotUsed() {
        string root = ArchitectureTestPaths.RepositoryRoot;
        string[] violations = SourceScanner.FindLinePatternViolations(
            root,
            ["global::" + "System."]);

        Assert.True(
            violations.Length == 0,
            "Use unqualified System type names when implicit usings already make them available. Violations:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void TargetTypedNew_IsNotUsedAsInvocationArgument() {
        string root = ArchitectureTestPaths.RepositoryRoot;
        string[] violations = [.. SourceScanner.SourceFiles(root)
            .Where(static path => !path.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .SelectMany(CSharpSyntaxReader.ReadTargetTypedNewInvocationArguments)
            .Select(location => location.Format(root))
            .OrderBy(static value => value, StringComparer.Ordinal)];

        Assert.True(
            violations.Length == 0,
            "Use explicit type names for object creation in method-call arguments. Keep target-typed new for collection/object initializers where the target type is obvious. Violations:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void ProductionCode_UsesTimeProviderInsteadOfDirectUtcNow() {
        string[] productionRoots = [.. ProjectReferenceReader.ReadProductionProjectNames()
            .Select(ProjectFolderFromProjectName)
            .Select(static folder => ArchitectureTestPaths.FromRoot(folder))];

        string[] violations = SourceScanner.FindLinePatternViolations(productionRoots, [
            "DateTime.UtcNow",
            "DateTimeOffset.UtcNow",
        ]);

        Assert.True(
            violations.Length == 0,
            "Inject TimeProvider instead of reading system UTC time directly in production code. Violations:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations));
    }

    private static string ProjectFolderFromProjectName(string projectName) =>
        string.Equals(projectName, "FoodDiary.Mediator", StringComparison.Ordinal)
            ? Path.Combine("Shared", "FoodDiary.Mediator")
            : projectName;
}
