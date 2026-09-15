using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class UsersAdministrationReaderOwnershipTests {
    [Fact]
    public void AdministrativeReads_BelongToUsers_WhileTrackedLookupAndWritesRemainSeparate() {
        string adapter = ArchitectureTestPaths.FromRoot("Modules/Users/Infrastructure/Persistence/Users/UserAdministrationReadRepository.cs");
        Assert.True(File.Exists(adapter));
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("Modules/Users/tests/FoodDiary.Modules.Users.Infrastructure.IntegrationTests/Integration/UserAdministrationReadRepositoryIntegrationTests.cs")));
        string[] repositoryMethods = ReadMethods("Modules/Users/Infrastructure/Persistence/Users/UserRepository.cs");
        string[] moduleMethods = ReadMethods("Modules/Users/Infrastructure/Persistence/Users/UserAdministrationReadRepository.cs");
        foreach (string method in new[] { "GetPagedAsync", "GetPagedReadModelsAsync", "GetByIdIncludingDeletedReadModelAsync", "GetAdminDashboardSummaryAsync", "GetAdminDashboardSummaryReadModelsAsync" }) {
            Assert.DoesNotContain(method, repositoryMethods, StringComparer.Ordinal);
            Assert.Contains(method, moduleMethods, StringComparer.Ordinal);
        }

        foreach (string method in new[] { "GetByIdAsync", "GetByGoogleIdentityIncludingDeletedAsync", "AddAsync", "UpdateAsync" }) {
            Assert.Contains(method, repositoryMethods, StringComparer.Ordinal);
            Assert.DoesNotContain(method, moduleMethods, StringComparer.Ordinal);
        }

        string centralRegistration = File.ReadAllText(ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure/DependencyInjection.cs"));
        string moduleRegistration = File.ReadAllText(ArchitectureTestPaths.FromRoot("Modules/Users/Infrastructure/UsersModuleRegistration.cs"));
        string[] centralNames = ReadIdentifiers(centralRegistration);
        string[] moduleNames = ReadIdentifiers(moduleRegistration);
        foreach (string port in new[] { "IUserAdminReadRepository", "IUserAdminReadModelRepository" }) {
            Assert.DoesNotContain(port, centralNames, StringComparer.Ordinal);
            Assert.Contains(port, moduleNames, StringComparer.Ordinal);
        }

        Assert.Contains("IUserRepository", moduleNames, StringComparer.Ordinal);
        Assert.Contains("IUserGoogleIdentityRepository", moduleNames, StringComparer.Ordinal);
        Assert.Contains("IUserAccessTokenSecurityReader", moduleNames, StringComparer.Ordinal);
    }

    private static string[] ReadMethods(string path) => [.. CSharpSyntaxTree.ParseText(
            File.ReadAllText(ArchitectureTestPaths.FromRoot(path))).GetRoot().DescendantNodes()
        .OfType<MethodDeclarationSyntax>().Select(method => method.Identifier.ValueText)];

    private static string[] ReadIdentifiers(string source) => [.. CSharpSyntaxTree.ParseText(source).GetRoot().DescendantTokens()
        .Where(token => token.RawKind == (int)SyntaxKind.IdentifierToken).Select(token => token.ValueText)];
}
