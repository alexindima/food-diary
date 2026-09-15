using System.Reflection;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class DailyDashboardDietologistRefactoringTests {
    [Fact]
    public void DietologistConsumerInterfaces_ContainOnlyTheReviewedAccessCapability() {
        var contracts = Assembly.Load("FoodDiary.Modules.Dietologist.Contracts");
        Assert.Equal(["IDietologistDashboardAccessService"], contracts.GetExportedTypes()
            .Where(type => type.IsInterface).Select(type => type.Name).Order(StringComparer.Ordinal), StringComparer.Ordinal);
        Type reminders = contracts.GetType("FoodDiary.Modules.Dietologist.Contracts.Commands.SendClientTaskReminders.SendClientTaskRemindersCommand", throwOnError: true)!;
        Assert.True(typeof(ITransactionalCommand).IsAssignableFrom(reminders));
    }

    [Theory]
    [InlineData("DailyAdvices", "DailyAdviceReadService")]
    [InlineData("Dietologist", "DietologistClientReadService")]
    [InlineData("Dietologist", "DietologistRecommendationReadService")]
    [InlineData("Dietologist", "RecommendationDiscussionReadService")]
    [InlineData("Dietologist", "RecommendationTemplateReadService")]
    [InlineData("Dietologist", "ClientTaskDueReminderProcessor")]
    [InlineData("Dietologist", "DietologistUserLookupService")]
    public void RetiredSingleUseServices_DoNotReturn(string module, string name) {
        string application = ArchitectureTestPaths.FromRoot("Modules", module, "Application");
        Assert.DoesNotContain(SourceScanner.SourceFiles(application), path =>
            string.Equals(Path.GetFileNameWithoutExtension(path), name, StringComparison.Ordinal) ||
            string.Equals(Path.GetFileNameWithoutExtension(path), "I" + name, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("Statistics")]
    [InlineData("WeeklyCheckIn")]
    public void StatisticsConsumers_UseTheOwnerRequest(string module) {
        string application = ArchitectureTestPaths.FromRoot("Modules", module, "Application");
        string source = string.Join(Environment.NewLine, SourceScanner.SourceFiles(application).Select(File.ReadAllText));
        Assert.Contains("ReadDashboardStatisticsQuery", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IDashboardStatisticsReadService", source, StringComparison.Ordinal);
    }
}
