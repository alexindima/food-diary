using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class UsersSecurityReaderOwnershipTests {
    [Fact]
    public void SecurityReader_AndFocusedProviderTests_BelongToUsers() {
        Assert.Multiple(
            () => Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("Modules/Users/Infrastructure/Persistence/Users/UserAccessTokenSecurityReader.cs"))),
            () => Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("Modules/Users/tests/FoodDiary.Modules.Users.Infrastructure.IntegrationTests/Integration/UserAccessTokenSecurityReaderIntegrationTests.cs"))));
        string[] repositoryIdentifiers = ReadIdentifiers("Modules/Users/Infrastructure/Persistence/Users/UserRepository.cs");
        string[] registrations = ReadIdentifiers("FoodDiary.Infrastructure/DependencyInjection.Repositories.cs");
        string[] moduleRegistrations = ReadIdentifiers("Modules/Users/Infrastructure/UsersModuleRegistration.cs");

        Assert.Multiple(
            () => Assert.DoesNotContain("IUserAccessTokenSecurityReader", repositoryIdentifiers, StringComparer.Ordinal),
            () => Assert.DoesNotContain("IsCurrentAsync", repositoryIdentifiers, StringComparer.Ordinal),
            () => Assert.DoesNotContain("IUserAccessTokenSecurityReader", registrations, StringComparer.Ordinal),
            () => Assert.Contains("IUserRepository", moduleRegistrations, StringComparer.Ordinal),
            () => Assert.Contains("IUserGoogleIdentityRepository", moduleRegistrations, StringComparer.Ordinal),
            () => Assert.Contains("IUserWriteRepository", moduleRegistrations, StringComparer.Ordinal),
            () => Assert.Contains("UserAccessTokenSecurityReader", moduleRegistrations, StringComparer.Ordinal));
    }

    [Theory]
    [InlineData("FoodDiary.Web.Api/Extensions/ApiServiceCollectionExtensions.cs")]
    [InlineData("FoodDiary.Initializer/Program.cs")]
    [InlineData("FoodDiary.JobManager/Program.cs")]
    public void Hosts_AlreadyComposeUsers(string path) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(path));
        string[] calls = [.. CSharpSyntaxTree.ParseText(source).GetRoot().DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Select(invocation => invocation.Expression)
            .OfType<MemberAccessExpressionSyntax>()
            .Select(access => access.Name.Identifier.ValueText)];

        Assert.Contains("AddUsersModule", calls, StringComparer.Ordinal);
    }

    private static string[] ReadIdentifiers(string path) => [.. CSharpSyntaxTree.ParseText(
            File.ReadAllText(ArchitectureTestPaths.FromRoot(path))).GetRoot().DescendantTokens()
        .Where(token => token.RawKind == (int)SyntaxKind.IdentifierToken)
        .Select(token => token.ValueText)];
}
