namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class ModuleOutboxOwnershipTests {
    [Theory]
    [InlineData("FoodDiary.Infrastructure/Persistence/Images/ImageObjectDeletionOutboxMessage.cs", "Modules/Images/PersistenceModel/Images/ImageObjectDeletionOutboxMessage.cs")]
    [InlineData("FoodDiary.Infrastructure/Persistence/Configurations/Images/ImageObjectDeletionOutboxMessageConfiguration.cs", "Modules/Images/PersistenceModel/Configurations/Images/ImageObjectDeletionOutboxMessageConfiguration.cs")]
    [InlineData("FoodDiary.Infrastructure/Persistence/Achievements/AchievementEvaluationOutboxMessage.cs", "Modules/Gamification/PersistenceModel/Achievements/AchievementEvaluationOutboxMessage.cs")]
    [InlineData("FoodDiary.Infrastructure/Persistence/Configurations/Achievements/AchievementEvaluationOutboxMessageConfiguration.cs", "Modules/Gamification/PersistenceModel/Configurations/Achievements/AchievementEvaluationOutboxMessageConfiguration.cs")]
    public void StreamRecordsAndMappings_StayInTheirModuleModel(string donorPath, string ownedPath) {
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot(donorPath)), $"Obsolete donor source: {donorPath}");
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot(ownedPath)), $"Missing module source: {ownedPath}");
    }
}
