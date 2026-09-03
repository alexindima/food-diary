using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class IdentityAuthenticationOwnershipTests {
    [Theory]
    [InlineData("FoodDiary.Infrastructure/Authentication/JwtTokenGenerator.cs", "Modules/Identity/Infrastructure/Authentication/JwtTokenGenerator.cs")]
    [InlineData("FoodDiary.Infrastructure/Services/PasswordHasher.cs", "Modules/Identity/Infrastructure/Services/PasswordHasher.cs")]
    [InlineData("tests/FoodDiary.Infrastructure.Tests/Authentication/JwtTokenGeneratorTests.cs", "Modules/Identity/tests/FoodDiary.Modules.Identity.Infrastructure.Tests/Authentication/JwtTokenGeneratorTests.cs")]
    [InlineData("tests/FoodDiary.Infrastructure.Tests/Services/PasswordHasherTests.cs", "Modules/Identity/tests/FoodDiary.Modules.Identity.Infrastructure.Tests/Authentication/PasswordHasherTests.cs")]
    public void AuthenticationAdaptersAndFocusedTests_StayWithIdentity(string donor, string owned) {
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot(donor)), $"Obsolete donor source: {donor}");
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot(owned)), $"Missing Identity source: {owned}");
    }

    [Theory]
    [InlineData("FoodDiary.Web.Api/Extensions/ApiServiceCollectionExtensions.cs")]
    [InlineData("FoodDiary.Initializer/Program.cs")]
    [InlineData("FoodDiary.JobManager/Program.cs")]
    public void Hosts_ComposeAuthenticationExplicitly(string path) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(path));
        string[] methods = [.. CSharpSyntaxTree.ParseText(source).GetRoot().DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Select(invocation => invocation.Expression)
            .OfType<MemberAccessExpressionSyntax>()
            .Select(access => access.Name.Identifier.ValueText)];

        Assert.Contains("AddIdentityAuthenticationInfrastructure", methods, StringComparer.Ordinal);
    }

    [Fact]
    public void CentralInfrastructure_KeepsOptionsButNotAuthenticationRegistrationsOrPackages() {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure/DependencyInjection.Authentication.cs"));
        string[] identifiers = [.. CSharpSyntaxTree.ParseText(source).GetRoot().DescendantTokens()
            .Where(token => token.RawKind == (int)SyntaxKind.IdentifierToken)
            .Select(token => token.ValueText)];
        string[] centralPackages = ReadPackages("FoodDiary.Infrastructure/FoodDiary.Infrastructure.csproj");
        string[] modulePackages = ReadPackages("Modules/Identity/Infrastructure/FoodDiary.Modules.Identity.Infrastructure.csproj");

        Assert.Multiple(
            () => Assert.DoesNotContain("JwtTokenGenerator", identifiers, StringComparer.Ordinal),
            () => Assert.DoesNotContain("PasswordHasher", identifiers, StringComparer.Ordinal),
            () => Assert.DoesNotContain("BCrypt.Net-Next", centralPackages, StringComparer.Ordinal),
            () => Assert.DoesNotContain("System.IdentityModel.Tokens.Jwt", centralPackages, StringComparer.Ordinal),
            () => Assert.Contains("BCrypt.Net-Next", modulePackages, StringComparer.Ordinal),
            () => Assert.Contains("System.IdentityModel.Tokens.Jwt", modulePackages, StringComparer.Ordinal),
            () => Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure/Options/JwtOptions.cs"))),
            () => Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("tests/FoodDiary.Infrastructure.Tests/Authentication/JwtOptionsTests.cs"))));
    }

    private static string[] ReadPackages(string path) => [.. XDocument.Load(ArchitectureTestPaths.FromRoot(path))
        .Descendants("PackageReference")
        .Select(element => element.Attribute("Include")?.Value ?? string.Empty)];
}
