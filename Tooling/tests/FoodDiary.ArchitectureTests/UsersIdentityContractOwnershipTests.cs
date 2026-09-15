using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class UsersIdentityContractOwnershipTests {
    [Theory]
    [InlineData("Modules/Users/Contracts")]
    [InlineData("Modules/Users/Application.Abstractions")]
    [InlineData("Modules/Identity/Application.Abstractions")]
    [InlineData("Modules/Identity/Contracts")]
    [InlineData("Modules/BodyMetrics/Contracts")]
    public void ContractSources_AreOwnedByTheDeclaredProject(string relativeRoot) {
        string root = ArchitectureTestPaths.FromRoot(relativeRoot);
        string project = Assert.Single(Directory.GetFiles(root, "*.csproj"));
        string namespaceRoot = Path.GetFileNameWithoutExtension(project);
        string[] sources = [.. SourceScanner.SourceFiles(root)];
        Assert.NotEmpty(sources);
        foreach (string source in sources.Where(path => Path.GetFileName(path) is not "AssemblyInfo.cs" and not "GlobalUsings.cs")) {
            string suffix = Path.GetDirectoryName(Path.GetRelativePath(root, source))!.Replace(Path.DirectorySeparatorChar, '.');
            Assert.Equal(string.IsNullOrEmpty(suffix) ? namespaceRoot : $"{namespaceRoot}.{suffix}", CSharpSyntaxReader.ReadNamespace(source));
        }
    }

    [Theory]
    [InlineData("Modules/Users/Contracts/Common/IUserTelegramAccountService.cs")]
    [InlineData("Modules/Users/Contracts/Models/UserTelegramRegistrationModel.cs")]
    [InlineData("Modules/Identity/Application.Abstractions/Authentication/Common/ITelegramOperationStore.cs")]
    [InlineData("Modules/Identity/Application.Abstractions/Authentication/Common/ITelegramLoginTicketStore.cs")]
    [InlineData("Modules/Identity/Application.Abstractions/Authentication/Abstractions/ITelegramOidcProvider.cs")]
    public void TelegramCapabilities_StayWithTheirDeclaredOwner(string path) {
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot(path)));
    }

    [Theory]
    [InlineData("Modules/Users/Contracts/Common/CurrentUserAccessResolver.cs")]
    [InlineData("Shared/FoodDiary.Authentication.Contracts/Authentication/Abstractions/IAdminSsoCodeStore.cs")]
    public void FormerCentralSeams_HaveExplicitNarrowOwners(string relativePath) {
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot(relativePath)));
        Assert.False(Directory.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Application.Abstractions")));
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
    [InlineData("Modules/Users/Application.Abstractions/FoodDiary.Modules.Users.Application.Abstractions.csproj")]
    [InlineData("Modules/Identity/Application.Abstractions/FoodDiary.Modules.Identity.Application.Abstractions.csproj")]
    public void OwnedContracts_NeverDependBackOnCentralOrImplementations(string project) {
        string[] references = ProjectReferenceReader.ReadProjectReferences(project);
        if (!project.StartsWith("Modules/Users/Contracts/", StringComparison.Ordinal)) {
            Assert.DoesNotContain("FoodDiary.Application.Contracts", references, StringComparer.Ordinal);
        }
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
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot($"Shared/FoodDiary.Application.Contracts/Common/Validation/{name}.cs")));
    }

    [Fact]
    public void Billing_DispatchesMarketingOwnedConversionRequest() {
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot("Modules/Billing/Contracts/Common/IBillingMarketingConversionRecorder.cs")));
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("Modules/Marketing/Contracts/Commands/RecordPremiumConversion/RecordPremiumConversionCommand.cs")));
        Assert.DoesNotContain("FoodDiary.Modules.Billing.Contracts",
            ProjectReferenceReader.ReadProjectReferences("Modules/Marketing/Application/FoodDiary.Modules.Marketing.Application.csproj"),
            StringComparer.Ordinal);
        Assert.Contains("FoodDiary.Modules.Marketing.Contracts",
            ProjectReferenceReader.ReadProjectReferences("Modules/Billing/Application/FoodDiary.Modules.Billing.Application.csproj"),
            StringComparer.Ordinal);
    }
}
