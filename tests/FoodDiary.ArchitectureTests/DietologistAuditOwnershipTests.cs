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
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure/DependencyInjection.Persistence.cs"));
        SyntaxNode syntax = CSharpSyntaxTree.ParseText(source).GetRoot();
        Assert.DoesNotContain(syntax.DescendantNodes().OfType<IdentifierNameSyntax>(),
            name => string.Equals(name.Identifier.ValueText, "CollaborationAuditInterceptor", StringComparison.Ordinal));
        Assert.Single(syntax.DescendantNodes().OfType<GenericNameSyntax>(), name =>
            string.Equals(name.Identifier.ValueText, "GetServices", StringComparison.Ordinal) &&
            name.TypeArgumentList.Arguments.SingleOrDefault() is IdentifierNameSyntax type &&
            string.Equals(type.Identifier.ValueText, "ISaveChangesInterceptor", StringComparison.Ordinal));
    }
}
