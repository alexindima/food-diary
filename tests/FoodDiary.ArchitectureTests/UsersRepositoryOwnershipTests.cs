using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class UsersRepositoryOwnershipTests {
    [Fact]
    public void TrackedRepositoryAndTests_BelongToUsers_WithoutCentralRegistrationOrImplicitSave() {
        Assert.Multiple(
            () => Assert.False(File.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure/Persistence/Users/UserRepository.cs"))),
            () => Assert.False(File.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure/DependencyInjection.Users.cs"))),
            () => Assert.False(File.Exists(ArchitectureTestPaths.FromRoot("tests/FoodDiary.Infrastructure.IntegrationTests/Integration/UserRepositoryIntegrationTests.cs"))),
            () => Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("Modules/Users/tests/FoodDiary.Modules.Users.Infrastructure.IntegrationTests/Integration/UserRepositoryIntegrationTests.cs"))));

        string repository = File.ReadAllText(ArchitectureTestPaths.FromRoot("Modules/Users/Infrastructure/Persistence/Users/UserRepository.cs"));
        string[] calls = [.. CSharpSyntaxTree.ParseText(repository).GetRoot().DescendantNodes()
            .OfType<InvocationExpressionSyntax>().Select(invocation => invocation.Expression)
            .OfType<MemberAccessExpressionSyntax>().Select(access => access.Name.Identifier.ValueText)];
        Assert.DoesNotContain("SaveChangesAsync", calls, StringComparer.Ordinal);
        Assert.DoesNotContain("SaveChanges", calls, StringComparer.Ordinal);
        Assert.DoesNotContain("BeginTransactionAsync", calls, StringComparer.Ordinal);
        string central = File.ReadAllText(ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure/DependencyInjection.cs"));
        string[] identifiers = [.. CSharpSyntaxTree.ParseText(central).GetRoot().DescendantTokens()
            .Where(token => token.RawKind == (int)SyntaxKind.IdentifierToken).Select(token => token.ValueText)];
        Assert.DoesNotContain("AddUserPersistence", identifiers, StringComparer.Ordinal);
        Assert.DoesNotContain("UserRepository", identifiers, StringComparer.Ordinal);
    }
}
