using FoodDiary.Modules.Billing.Domain.Contracts;
using FoodDiary.Modules.ContentReports.Domain.Contracts.Enums;
using FoodDiary.Modules.ContentReports.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Gamification.Domain.Contracts.Entities.Achievements;
using FoodDiary.Modules.Gamification.Domain.Contracts.Enums;

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
    [InlineData(typeof(BillingProviderNames), "Billing", "")]
    [InlineData(typeof(ReportStatus), "ContentReports", "Enums")]
    [InlineData(typeof(ReportTargetType), "ContentReports", "Enums")]
    [InlineData(typeof(AchievementMetric), "Gamification", "Enums")]
    [InlineData(typeof(AchievementDefinitionLimits), "Gamification", "Entities/Achievements")]
    [InlineData(typeof(ContentReportId), "ContentReports", "ValueObjects/Ids")]
    public void Scalars_HaveOneNarrowOwner(Type type, string owner, string folder) {
        string projectName = $"FoodDiary.Modules.{owner}.Domain.Contracts";
        Assert.Equal(projectName, type.Assembly.GetName().Name);
        string[] expectedReferences = owner switch {
            "Billing" => ["FoodDiary.Domain.Primitives"],
            "ContentReports" => ["FoodDiary.Domain.Primitives", "FoodDiary.Modules.RecipeCommunity.Domain.Contracts", "FoodDiary.Modules.Recipes.Domain.Contracts"],
            "Gamification" => [],
            _ => throw new ArgumentOutOfRangeException(nameof(owner)),
        };
        Assert.Equal(expectedReferences, ProjectReferenceReader.ReadProjectReferences(
            $"Modules/{owner}/Domain.Contracts/{projectName}.csproj"));
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot(
            $"Modules/{owner}/Domain.Contracts/{folder}/{type.Name}.cs")));
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot(
            $"Modules/{owner}/Domain/{folder}/{type.Name}.cs")));

        string[] expectedTypes = owner switch {
            "Billing" => ["FoodDiary.Modules.Billing.Domain.Contracts.BillingPremiumAccessPolicy", "FoodDiary.Modules.Billing.Domain.Contracts.BillingProviderNames", "FoodDiary.Modules.Billing.Domain.Contracts.ValueObjects.Ids.BillingPaymentId", "FoodDiary.Modules.Billing.Domain.Contracts.ValueObjects.Ids.BillingSubscriptionId", "FoodDiary.Modules.Billing.Domain.Contracts.ValueObjects.Ids.BillingWebhookEventId"],
            "ContentReports" => ["FoodDiary.Modules.ContentReports.Domain.Contracts.Enums.ReportStatus", "FoodDiary.Modules.ContentReports.Domain.Contracts.Enums.ReportTargetType", "FoodDiary.Modules.ContentReports.Domain.Contracts.ValueObjects.Ids.ContentReportId", "FoodDiary.Modules.ContentReports.Domain.Contracts.ValueObjects.ReportTarget"],
            "Gamification" => ["FoodDiary.Modules.Gamification.Domain.Contracts.Entities.Achievements.AchievementDefinitionLimits", "FoodDiary.Modules.Gamification.Domain.Contracts.Enums.AchievementMetric"],
            _ => throw new ArgumentOutOfRangeException(nameof(owner)),
        };
        Assert.Equal(expectedTypes, type.Assembly.GetExportedTypes()
            .Select(exported => exported.FullName).Order(StringComparer.Ordinal), StringComparer.Ordinal);
    }
}
