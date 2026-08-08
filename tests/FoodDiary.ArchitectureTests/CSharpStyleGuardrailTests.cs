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

}
