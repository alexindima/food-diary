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
            .Where(static path => path.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) is false)
            .SelectMany(CSharpSyntaxReader.ReadTargetTypedNewInvocationArguments)
            .Select(location => location.Format(root))
            .OrderBy(static value => value, StringComparer.Ordinal)];

        Assert.True(
            violations.Length == 0,
            "Use explicit type names for object creation in method-call arguments. Keep target-typed new for collection/object initializers where the target type is obvious. Violations:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations));
    }
}
