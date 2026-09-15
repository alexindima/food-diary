namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class SharedRuntimeBoundaryTests {
    [Theory]
    [InlineData("EfUnitOfWork.cs")]
    [InlineData("Audit/AuditEntryService.cs")]
    [InlineData("Email/EmailOutbox.cs")]
    [InlineData("Email/EmailOutboxProcessor.cs")]
    [InlineData("Email/EmailOutboxReplayStream.cs")]
    [InlineData("Outbox/OutboxDeadLetterReplayService.cs")]
    [InlineData("Shared/EfModuleTransactionCoordinator.cs")]
    [InlineData("Shared/EfModuleSessionCoordinator.cs")]
    [InlineData("Shared/EfModuleScopeGuard.cs")]
    [InlineData("Shared/EfModuleSessionLock.cs")]
    [InlineData("Shared/EfIndependentModuleContextOptionsFactory.cs")]
    [InlineData("Shared/ModuleContextSaveCoordinator.cs")]
    [InlineData("Shared/PersistenceSession.cs")]
    public void RuntimeServicesDoNotDependOnFullMigrationContext(string relativePath) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure", "Persistence", relativePath));
        Assert.DoesNotContain("FoodDiaryDbContext", source, StringComparison.Ordinal);
    }

    [Fact]
    public void SharedRuntimeModelDoesNotApplyModuleModels() {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure", "Persistence", "SharedPersistenceDbContext.cs"));
        Assert.DoesNotContain("FoodDiary.Modules.", source, StringComparison.Ordinal);
        Assert.Contains("ApplyAuditPersistenceModel", source, StringComparison.Ordinal);
        Assert.Contains("ApplyEmailPersistenceModel", source, StringComparison.Ordinal);
        Assert.Contains("ApplyOutboxPersistenceModel", source, StringComparison.Ordinal);
    }
}
