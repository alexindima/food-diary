using System.Xml.Linq;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class CentralPersistenceOwnershipTests {
    [Fact]
    public void SharedTechnicalAdapters_AreOutsideFullModelAssembly() {
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("Shared/FoodDiary.Persistence.Runtime/FoodDiary.Persistence.Runtime.csproj")));
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Persistence.Runtime/FoodDiary.Persistence.Runtime.csproj")));
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure/Authentication/InMemoryAdminSsoCodeStore.cs")));
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure/Services/StructuredAuditLogger.cs")));
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure/DependencyInjection.Options.cs")));
        string[] references = [.. XDocument.Load(ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure/FoodDiary.Infrastructure.csproj"))
            .Descendants("ProjectReference").Select(element => Path.GetFileNameWithoutExtension(element.Attribute("Include")!.Value))];
        Assert.DoesNotContain("FoodDiary.Authentication.Contracts", references, StringComparer.Ordinal);
        Assert.DoesNotContain("FoodDiary.Authentication.Infrastructure", references, StringComparer.Ordinal);
        Assert.DoesNotContain("FoodDiary.Email.Contracts", references, StringComparer.Ordinal);
        Assert.DoesNotContain("FoodDiary.Audit.Contracts", references, StringComparer.Ordinal);
        Assert.DoesNotContain("FoodDiary.Audit.Infrastructure", references, StringComparer.Ordinal);
        Assert.DoesNotContain("FoodDiary.Email.Infrastructure", references, StringComparer.Ordinal);
    }

    [Theory]
    [InlineData("FoodDiary.Web.Api/Extensions/ApiServiceCollectionExtensions.cs")]
    [InlineData("FoodDiary.JobManager/Program.cs")]
    [InlineData("FoodDiary.Initializer/Program.cs")]
    public void Hosts_RegisterSharedAuthenticationAndIdentityEmailOptions(string path) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(path));
        Assert.Contains("AddSharedAuthentication(", source, StringComparison.Ordinal);
        Assert.Contains("AddIdentityEmailOptions(", source, StringComparison.Ordinal);
        Assert.Contains("AddAuditInfrastructure(", source, StringComparison.Ordinal);
        Assert.Contains("AddEmailInfrastructure(", source, StringComparison.Ordinal);
        Assert.Contains("AddOutboxReplayManagement(", source, StringComparison.Ordinal);
        Assert.Contains("AddOutboxProcessing(", source, StringComparison.Ordinal);
    }
}
