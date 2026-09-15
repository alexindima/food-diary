namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class SharedRuntimeBoundaryTests {
    [Theory]
    [InlineData("db.Users.AsTracking().ToList()", "tracked:User")]
    [InlineData("db.Users.AsNoTracking().ExecuteDelete()", "write:User")]
    public void ReadFacadeDoesNotHideForbiddenEfCapabilities(string query, string expected) {
        string source = "using FoodDiary.Infrastructure.Persistence; using Microsoft.EntityFrameworkCore; "
            + "class Probe(ICompositionReadContext db) { public void Run() { _ = " + query + "; } }";
        IReadOnlyDictionary<string, string[]> capabilities = PersistenceCapabilityScanner.Scan([("Probe.cs", source)]);
        Assert.Contains(expected, capabilities["Probe.cs"], StringComparer.Ordinal);
    }

    [Fact]
    public void RuntimeDependencyClosureExcludesFullModelAndModuleImplementations() {
        IReadOnlyDictionary<string, string[]> graph = ProjectReferenceReader.ReadProductionProjectReferences();
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var pending = new Stack<string>();
        pending.Push("FoodDiary.Persistence.Runtime");
        while (pending.TryPop(out string? project)) {
            if (!visited.Add(project)) { continue; }
            Assert.NotEqual("FoodDiary.Infrastructure", project, StringComparer.Ordinal);
            Assert.NotEqual("FoodDiary.ReadModel.Composition", project, StringComparer.Ordinal);
            if (project.StartsWith("FoodDiary.Modules.", StringComparison.Ordinal)) {
                // Shared audit records consume UserId, not the Users aggregate or storage.
                Assert.Equal("FoodDiary.Modules.Users.Domain.Contracts", project, StringComparer.Ordinal);
            }
            foreach (string dependency in graph[project]) { pending.Push(dependency); }
        }
        Assert.Contains("FoodDiary.Persistence.Abstractions", visited);
        Assert.DoesNotContain("FoodDiary.Audit.Infrastructure", visited);
        Assert.DoesNotContain("FoodDiary.Email.Infrastructure", visited);
    }

    [Fact]
    public void ReadersCannotRequestWritableContextExceptAtRegistrationBoundary() {
        string root = ArchitectureTestPaths.FromRoot("FoodDiary.ReadModel.Composition");
        foreach (string path in SourceScanner.SourceFiles(root)
                     .Where(path => !path.EndsWith("ReadModelCompositionRegistration.cs", StringComparison.Ordinal))) {
            string source = File.ReadAllText(path);
            Assert.DoesNotContain("FoodDiaryDbContext", source, StringComparison.Ordinal);
            Assert.DoesNotContain("DbSet<", source, StringComparison.Ordinal);
            Assert.DoesNotContain("DbContext ", source, StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData("EfUnitOfWork.cs")]
    [InlineData("Outbox/OutboxDeadLetterReplayService.cs")]
    [InlineData("Shared/EfModuleTransactionCoordinator.cs")]
    [InlineData("Shared/EfModuleSessionCoordinator.cs")]
    [InlineData("Shared/EfModuleScopeGuard.cs")]
    [InlineData("Shared/EfModuleSessionLock.cs")]
    [InlineData("Shared/EfIndependentModuleContextOptionsFactory.cs")]
    [InlineData("Shared/ModuleContextSaveCoordinator.cs")]
    [InlineData("Shared/PersistenceSession.cs")]
    public void RuntimeServicesDoNotDependOnFullMigrationContext(string relativePath) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot("Shared", "FoodDiary.Persistence.Runtime", "Persistence", relativePath));
        Assert.DoesNotContain("FoodDiaryDbContext", source, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("FoodDiary.Audit.Infrastructure", "AuditEntryService.cs")]
    [InlineData("FoodDiary.Email.Infrastructure", "EmailOutbox.cs")]
    [InlineData("FoodDiary.Email.Infrastructure", "EmailOutboxProcessor.cs")]
    [InlineData("FoodDiary.Email.Infrastructure", "EmailOutboxReplayStream.cs")]
    public void OptionalAdaptersDoNotDependOnFullMigrationContext(string project, string file) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot("Shared", project, "Persistence", file));
        Assert.DoesNotContain("FoodDiaryDbContext", source, StringComparison.Ordinal);
        Assert.DoesNotContain(project, ProjectReferenceReader.ReadProductionProjectReferences()["FoodDiary.Persistence.Runtime"], StringComparer.Ordinal);
    }

    [Fact]
    public void SharedRuntimeModelDoesNotApplyModuleModels() {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot("Shared", "FoodDiary.Persistence.Runtime", "Persistence", "SharedPersistenceDbContext.cs"));
        Assert.DoesNotContain("FoodDiary.Modules.", source, StringComparison.Ordinal);
        Assert.Contains("ApplyAuditPersistenceModel", source, StringComparison.Ordinal);
        Assert.Contains("ApplyEmailPersistenceModel", source, StringComparison.Ordinal);
        Assert.Contains("ApplyOutboxPersistenceModel", source, StringComparison.Ordinal);
    }
}
