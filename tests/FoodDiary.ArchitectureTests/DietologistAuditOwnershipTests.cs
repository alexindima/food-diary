using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class DietologistAuditOwnershipTests {
    [Theory]
    [InlineData("FoodDiary.Infrastructure/Persistence/Interceptors/CollaborationAuditInterceptor.cs", "Modules/Dietologist/Infrastructure/Persistence/Interceptors/CollaborationAuditInterceptor.cs")]
    [InlineData("tests/FoodDiary.Infrastructure.Tests/Persistence/CollaborationAuditInterceptorTests.cs", "Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Infrastructure.Tests/Persistence/CollaborationAuditInterceptorTests.cs")]
    public void CollaborationRulesAndFocusedTests_BelongToDietologist(string donor, string owner) {
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot(donor)), $"Obsolete donor: {donor}");
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot(owner)), $"Missing Dietologist source: {owner}");
    }

    [Fact]
    public void CentralPersistence_UsesEfPortNotDietologistImplementation() {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot("Shared/FoodDiary.Persistence.Runtime/PersistenceRuntimeRegistration.cs"));
        SyntaxNode syntax = CSharpSyntaxTree.ParseText(source).GetRoot();
        Assert.DoesNotContain(syntax.DescendantNodes().OfType<IdentifierNameSyntax>(),
            name => string.Equals(name.Identifier.ValueText, "CollaborationAuditInterceptor", StringComparison.Ordinal));
        Assert.Single(syntax.DescendantNodes().OfType<GenericNameSyntax>(), name =>
            string.Equals(name.Identifier.ValueText, "GetServices", StringComparison.Ordinal) &&
            name.TypeArgumentList.Arguments.SingleOrDefault() is IdentifierNameSyntax type &&
            string.Equals(type.Identifier.ValueText, "ISaveChangesInterceptor", StringComparison.Ordinal));
    }

    [Fact]
    public void ModuleTrackerInspection_RemainsLimitedToDietologistAudit() {
        string[] consumers = [.. SourceScanner.SourceFiles(ArchitectureTestPaths.FromRoot("Modules"))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}tests{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => File.ReadAllText(path).Contains("GetModuleEntries", StringComparison.Ordinal))
            .Where(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path)).GetRoot()
                .DescendantNodes().OfType<GenericNameSyntax>()
                .Any(name => name.Identifier.ValueText.Equals("GetModuleEntries", StringComparison.Ordinal)))];
        string consumer = Assert.Single(consumers);
        Assert.Equal(ArchitectureTestPaths.FromRoot("Modules", "Dietologist", "Infrastructure", "Persistence", "Interceptors", "CollaborationAuditInterceptor.cs"), consumer);
        Assert.Contains("GetModuleEntries<DietologistDbContext>()", File.ReadAllText(consumer), StringComparison.Ordinal);
    }
}
