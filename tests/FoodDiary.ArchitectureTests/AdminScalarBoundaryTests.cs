namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class AdminScalarBoundaryTests {
    [Theory]
    [InlineData("Billing")]
    [InlineData("ContentReports")]
    [InlineData("Gamification")]
    public void Admin_UsesScalarOwnerWithoutAggregateDomain(string owner) {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "Modules/Admin/Application/FoodDiary.Modules.Admin.Application.csproj");

        Assert.Contains($"FoodDiary.Modules.{owner}.Domain.Contracts", references, StringComparer.Ordinal);
        Assert.DoesNotContain($"FoodDiary.Modules.{owner}.Domain", references, StringComparer.Ordinal);
    }

    [Fact]
    public void ModerationContracts_DoNotExposeAggregateDomain() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "Modules/ContentReports/Contracts/FoodDiary.Modules.ContentReports.Contracts.csproj");

        Assert.Contains("FoodDiary.Modules.ContentReports.Domain.Contracts", references, StringComparer.Ordinal);
        Assert.DoesNotContain("FoodDiary.Modules.ContentReports.Domain", references, StringComparer.Ordinal);
    }

    [Theory]
    [InlineData(typeof(FoodDiary.Modules.Billing.Domain.Contracts.BillingProviderNames), "Billing", "")]
    [InlineData(typeof(FoodDiary.Modules.ContentReports.Domain.Contracts.Enums.ReportStatus), "ContentReports", "Enums")]
    [InlineData(typeof(FoodDiary.Modules.ContentReports.Domain.Contracts.Enums.ReportTargetType), "ContentReports", "Enums")]
    [InlineData(typeof(FoodDiary.Modules.Gamification.Domain.Contracts.Enums.AchievementMetric), "Gamification", "Enums")]
    [InlineData(typeof(FoodDiary.Modules.Gamification.Domain.Contracts.Entities.Achievements.AchievementDefinitionLimits), "Gamification", "Entities/Achievements")]
    [InlineData(typeof(FoodDiary.Modules.ContentReports.Domain.Contracts.ValueObjects.Ids.ContentReportId), "ContentReports", "ValueObjects/Ids")]
    public void Scalars_HaveOneNarrowOwner(Type type, string owner, string folder) {
        string projectName = $"FoodDiary.Modules.{owner}.Domain.Contracts";
        Assert.Equal(projectName, type.Assembly.GetName().Name);
        string[] expectedReferences = string.Equals(owner, "ContentReports", StringComparison.Ordinal)
            ? ["FoodDiary.Domain.Primitives"] : [];
        Assert.Equal(expectedReferences, ProjectReferenceReader.ReadProjectReferences(
            $"Modules/{owner}/Domain.Contracts/{projectName}.csproj"));
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot(
            $"Modules/{owner}/Domain.Contracts/{folder}/{type.Name}.cs")));
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot(
            $"Modules/{owner}/Domain/{folder}/{type.Name}.cs")));

        string[] expectedTypes = owner switch {
            "Billing" => ["FoodDiary.Modules.Billing.Domain.Contracts.BillingPremiumAccessPolicy", "FoodDiary.Modules.Billing.Domain.Contracts.BillingProviderNames"],
            "ContentReports" => ["FoodDiary.Modules.ContentReports.Domain.Contracts.Enums.ReportStatus", "FoodDiary.Modules.ContentReports.Domain.Contracts.Enums.ReportTargetType", "FoodDiary.Modules.ContentReports.Domain.Contracts.ValueObjects.Ids.ContentReportId"],
            "Gamification" => ["FoodDiary.Modules.Gamification.Domain.Contracts.Entities.Achievements.AchievementDefinitionLimits", "FoodDiary.Modules.Gamification.Domain.Contracts.Enums.AchievementMetric"],
            _ => throw new ArgumentOutOfRangeException(nameof(owner)),
        };
        Assert.Equal(expectedTypes, type.Assembly.GetExportedTypes()
            .Select(exported => exported.FullName).Order(StringComparer.Ordinal), StringComparer.Ordinal);
    }
}
