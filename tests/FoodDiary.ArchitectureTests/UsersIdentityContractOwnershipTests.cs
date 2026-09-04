using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class UsersIdentityContractOwnershipTests {
    [Theory]
    [InlineData("Modules/Users/Contracts", 59)]
    [InlineData("Modules/Users/Application/Abstractions", 7)]
    [InlineData("Modules/Identity/Application/Abstractions", 45)]
    public void ContractSources_AreOwnedByTheDeclaredProject(string relativeRoot, int count) {
        Assert.Equal(count, SourceScanner.SourceFiles(ArchitectureTestPaths.FromRoot(relativeRoot)).Count());
    }

    [Theory]
    [InlineData("Users", "Common/CurrentUserAccessResolver.cs")]
    [InlineData("Authentication", "Abstractions/IAdminSsoCodeStore.cs")]
    [InlineData("Admin", null)]
    [InlineData("Billing", null)]
    public void CentralAreas_RetainOnlyExplicitSharedSeams(string area, string? retainedFile) {
        string root = ArchitectureTestPaths.FromRoot($"FoodDiary.Application.Abstractions/{area}");
        string[] actual = Directory.Exists(root)
            ? [.. SourceScanner.SourceFiles(root).Select(path => Path.GetRelativePath(root, path).Replace('\\', '/')).Order(StringComparer.Ordinal)]
            : [];
        string[] expected = retainedFile is null ? [] : [retainedFile];
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UsersPublicContracts_DoNotExposeAggregatesOrRepositories() {
        string root = ArchitectureTestPaths.FromRoot("Modules/Users/Contracts");
        string[] identifiers = [.. SourceScanner.SourceFiles(root)
            .SelectMany(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path)).GetRoot()
                .DescendantNodes().OfType<IdentifierNameSyntax>())
            .Select(node => node.Identifier.ValueText)];
        foreach (string aggregate in new[] { "User", "Role", "UserRole", "UserRoleAuditEvent" }) {
            Assert.DoesNotContain(aggregate, identifiers, StringComparer.Ordinal);
        }
        Assert.DoesNotContain(identifiers, name => name.EndsWith("Repository", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("Modules/Users/Contracts/FoodDiary.Modules.Users.Contracts.csproj")]
    [InlineData("Modules/Users/Application/Abstractions/FoodDiary.Modules.Users.Application.Abstractions.csproj")]
    [InlineData("Modules/Identity/Application/Abstractions/FoodDiary.Modules.Identity.Application.Abstractions.csproj")]
    public void OwnedContracts_NeverDependBackOnCentralOrImplementations(string project) {
        string[] references = ProjectReferenceReader.ReadProjectReferences(project);
        Assert.DoesNotContain("FoodDiary.Application.Abstractions", references, StringComparer.Ordinal);
        Assert.DoesNotContain(references, name => name.EndsWith(".Infrastructure", StringComparison.Ordinal) ||
            name.EndsWith(".Application", StringComparison.Ordinal) || name.StartsWith("FoodDiary.Web.", StringComparison.Ordinal));
        if (project.StartsWith("Modules/Users/", StringComparison.Ordinal)) {
            Assert.DoesNotContain(references, name => name.StartsWith("FoodDiary.Modules.Identity.", StringComparison.Ordinal));
        }
    }

    [Theory]
    [InlineData("DietologistRequiredIdParser")]
    [InlineData("DietologistEnumValueParser")]
    public void DietologistValidation_HasOnePhysicalOwner(string name) {
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot($"Modules/Dietologist/Application/Common/Validation/{name}.cs")));
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot($"FoodDiary.Application.Abstractions/Common/Validation/{name}.cs")));
    }

    [Fact]
    public void Marketing_ExplicitlyConsumesBillingOwnedConversionPort() {
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("Modules/Billing/Application/Abstractions/Common/IBillingMarketingConversionRecorder.cs")));
        Assert.Contains("FoodDiary.Modules.Billing.Application.Abstractions",
            ProjectReferenceReader.ReadProjectReferences("Modules/Marketing/Application/FoodDiary.Application.Marketing.csproj"),
            StringComparer.Ordinal);
    }
}
