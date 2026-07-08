using System.Globalization;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class SideEffectReliabilityGuardrailTests {
    [Fact]
    public void PostCommitQueueContract_DocumentsBestEffortSemantics() {
        string contractPath = ArchitectureTestPaths.FromRoot(
            "FoodDiary.Application.Abstractions",
            "Common",
            "Abstractions",
            "Persistence",
            "IPostCommitActionQueue.cs");
        string source = File.ReadAllText(contractPath);

        Assert.Contains("best-effort", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not a durable delivery mechanism", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("transactional outbox", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ApplicationPostCommitActions_AreNamedForOperationalLogs() {
        string root = ArchitectureTestPaths.RepositoryRoot;
        string applicationRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application");

        string[] violations = [.. SourceScanner.SourceFiles(applicationRoot)
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { path, index, line = line.Trim() }))
            .Where(static entry => entry.line.Contains("postCommitActionQueue.Enqueue(", StringComparison.Ordinal))
            .Where(static entry => !entry.line.Contains("postCommitActionQueue.Enqueue(\"", StringComparison.Ordinal))
            .Select(entry => string.Create(
                CultureInfo.InvariantCulture,
                $"{Path.GetRelativePath(root, entry.path)}:{entry.index + 1}"))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void PostCommitActionCallSites_DoNotDependOnCriticalDurableSideEffectServices() {
        string root = ArchitectureTestPaths.RepositoryRoot;
        string applicationRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application");
        string[] forbiddenPatterns = [
            "IEmailOutbox",
            "IEmailSender",
            "IDietologistEmailSender",
            "IEmailTransport",
            "IImageObjectDeletionOutbox",
            "IImageObjectDeletionOutboxProcessor",
            "INotificationWebPushOutbox",
            "INotificationWebPushOutboxProcessor",
        ];

        string[] violations = [.. SourceScanner.SourceFiles(applicationRoot)
            .Where(path => File.ReadAllText(path).Contains("postCommitActionQueue.Enqueue(", StringComparison.Ordinal))
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { path, index, line }))
            .Where(entry => forbiddenPatterns.Any(pattern => entry.line.Contains(pattern, StringComparison.Ordinal)))
            .Select(entry => string.Create(
                CultureInfo.InvariantCulture,
                $"{Path.GetRelativePath(root, entry.path)}:{entry.index + 1}"))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void OutboxProcessors_UseSharedProcessingPolicyForRetriesAndErrorTruncation() {
        string root = ArchitectureTestPaths.RepositoryRoot;
        string persistenceRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure", "Persistence");
        string policyPath = Path.Combine(persistenceRoot, "Outbox", "OutboxProcessingPolicy.cs");
        string[] forbiddenPatterns = [
            "private static TimeSpan CalculateRetryDelay",
            "private static string TruncateError",
            "Math.Pow(2, Math.Min(attemptCount",
            "error[..MaxErrorLength]",
        ];

        string[] violations = [.. SourceScanner.SourceFiles(persistenceRoot)
            .Where(path => !string.Equals(path, policyPath, StringComparison.OrdinalIgnoreCase))
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { path, index, line }))
            .Where(entry => forbiddenPatterns.Any(pattern => entry.line.Contains(pattern, StringComparison.Ordinal)))
            .Select(entry => string.Create(
                CultureInfo.InvariantCulture,
                $"{Path.GetRelativePath(root, entry.path)}:{entry.index + 1}"))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void OutboxMessageClaimer_DoesNotUseReflectionForMessageOperations() {
        string claimerPath = ArchitectureTestPaths.FromRoot(
            "FoodDiary.Infrastructure",
            "Persistence",
            "Outbox",
            "OutboxMessageClaimer.cs");
        string source = File.ReadAllText(claimerPath);

        Assert.DoesNotContain("System.Reflection", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GetMethod(", source, StringComparison.Ordinal);
        Assert.DoesNotContain(".Invoke(", source, StringComparison.Ordinal);
        Assert.Contains("where TMessage : class, IOutboxMessage", source, StringComparison.Ordinal);
    }

    [Fact]
    public void InfrastructureOutboxMessages_ImplementOutboxMessageContract() {
        string root = ArchitectureTestPaths.RepositoryRoot;
        string persistenceRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure", "Persistence");
        string[] outboxMessageFiles = [.. SourceScanner.SourceFiles(persistenceRoot)
            .Where(static path => Path.GetFileName(path).EndsWith("OutboxMessage.cs", StringComparison.Ordinal) &&
                                  !string.Equals(Path.GetFileName(path), "IOutboxMessage.cs", StringComparison.Ordinal))];

        string[] violations = [.. outboxMessageFiles
            .Where(path => !File.ReadAllText(path).Contains(": IOutboxMessage", StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(root, path))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }
    [Fact]
    public void OutboxClaiming_ExcludesDeadLetteredMessages() {
        string claimerPath = ArchitectureTestPaths.FromRoot(
            "FoodDiary.Infrastructure",
            "Persistence",
            "Outbox",
            "OutboxMessageClaimer.cs");
        string source = File.ReadAllText(claimerPath);

        Assert.Contains("AND \"DeadLetteredOnUtc\" IS NULL", source, StringComparison.Ordinal);
        Assert.Contains("EF.Property<DateTime?>(message, \"DeadLetteredOnUtc\") == null", source, StringComparison.Ordinal);
    }

    [Fact]
    public void OutboxProcessors_DeadLetterAfterSharedMaxAttempts() {
        string root = ArchitectureTestPaths.RepositoryRoot;
        string persistenceRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure", "Persistence");
        string[] processorFiles = [.. SourceScanner.SourceFiles(persistenceRoot)
            .Where(static path => Path.GetFileName(path).EndsWith("OutboxProcessor.cs", StringComparison.Ordinal))];

        string[] violations = [.. processorFiles
            .Where(path => {
                string source = File.ReadAllText(path);
                return !source.Contains("OutboxProcessingPolicy.ShouldDeadLetter", StringComparison.Ordinal) ||
                       !source.Contains("MarkDeadLettered", StringComparison.Ordinal) ||
                       !source.Contains("OutboxProcessingPolicy.MaxAttemptCount", StringComparison.Ordinal);
            })
            .Select(path => Path.GetRelativePath(root, path))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void InfrastructureOutboxMessages_ExposeDeadLetterTimestamp() {
        string root = ArchitectureTestPaths.RepositoryRoot;
        string persistenceRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure", "Persistence");
        string[] outboxMessageFiles = [.. SourceScanner.SourceFiles(persistenceRoot)
            .Where(static path => Path.GetFileName(path).EndsWith("OutboxMessage.cs", StringComparison.Ordinal) &&
                                  !string.Equals(Path.GetFileName(path), "IOutboxMessage.cs", StringComparison.Ordinal))];

        string[] violations = [.. outboxMessageFiles
            .Where(path => !File.ReadAllText(path).Contains("DeadLetteredOnUtc", StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(root, path))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }
    [Fact]
    public void OutboxProcessors_RecordOperationalTelemetry() {
        string root = ArchitectureTestPaths.RepositoryRoot;
        string persistenceRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure", "Persistence");
        string[] processorFiles = [.. SourceScanner.SourceFiles(persistenceRoot)
            .Where(static path => Path.GetFileName(path).EndsWith("OutboxProcessor.cs", StringComparison.Ordinal))];

        string[] requiredPatterns = [
            "RecordOutboxMessages(OutboxName, \"claimed\"",
            "RecordOutboxMessages(OutboxName, \"processed\"",
            "RecordOutboxMessages(OutboxName, \"retried\"",
            "RecordOutboxMessages(OutboxName, \"dead_lettered\"",
            "RecordOutboxProcessingDuration(OutboxName",
        ];

        string[] violations = [.. processorFiles
            .Where(path => {
                string source = File.ReadAllText(path);
                return requiredPatterns.Any(pattern => !source.Contains(pattern, StringComparison.Ordinal));
            })
            .Select(path => Path.GetRelativePath(root, path))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void InfrastructureTelemetry_DefinesOutboxOperationalMetrics() {
        string telemetryPath = ArchitectureTestPaths.FromRoot(
            "FoodDiary.Infrastructure",
            "Services",
            "InfrastructureTelemetry.cs");
        string source = File.ReadAllText(telemetryPath);

        Assert.Contains("fooddiary.outbox.messages", source, StringComparison.Ordinal);
        Assert.Contains("fooddiary.outbox.processing.duration", source, StringComparison.Ordinal);
        Assert.Contains("fooddiary.outbox.name", source, StringComparison.Ordinal);
        Assert.Contains("fooddiary.outbox.outcome", source, StringComparison.Ordinal);
    }
    [Fact]
    public void EventTaxonomyContracts_DocumentDomainAndIntegrationEventBoundaries() {
        string domainEventPath = ArchitectureTestPaths.FromRoot(
            "Shared",
            "FoodDiary.Domain.Primitives",
            "IDomainEvent.cs");
        string integrationEventPath = ArchitectureTestPaths.FromRoot(
            "FoodDiary.Application.Abstractions",
            "Common",
            "Abstractions",
            "Events",
            "IIntegrationEvent.cs");
        string taxonomyPath = ArchitectureTestPaths.FromRoot(
            "docs",
            "backend",
            "BACKEND_EVENT_TAXONOMY.md");

        string domainEventSource = File.ReadAllText(domainEventPath);
        string integrationEventSource = File.ReadAllText(integrationEventPath);
        string taxonomySource = File.ReadAllText(taxonomyPath);

        Assert.Contains("committing the current transaction", domainEventSource, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("must not call", domainEventSource, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("committed application fact", integrationEventSource, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("durable outbox", integrationEventSource, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Domain Event", taxonomySource, StringComparison.Ordinal);
        Assert.Contains("Integration Event", taxonomySource, StringComparison.Ordinal);
        Assert.Contains("Outbox Message", taxonomySource, StringComparison.Ordinal);
        Assert.Contains("Post-Commit Action", taxonomySource, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplicationLayer_DoesNotDeclareDomainEvents() {
        string root = ArchitectureTestPaths.RepositoryRoot;
        string applicationRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application");

        string[] violations = [.. SourceScanner.SourceFiles(applicationRoot)
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { path, index, line }))
            .Where(static entry => entry.line.Contains(": IDomainEvent", StringComparison.Ordinal) ||
                                   entry.line.Contains("DomainEvent :", StringComparison.Ordinal))
            .Select(entry => string.Create(
                CultureInfo.InvariantCulture,
                $"{Path.GetRelativePath(root, entry.path)}:{entry.index + 1}"))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void CriticalSideEffectServices_WriteDurableOutboxState() {
        string emailSenderPath = ArchitectureTestPaths.FromRoot(
            "FoodDiary.Application",
            "Authentication",
            "Services",
            "EmailSender.cs");
        string dietologistEmailSenderPath = ArchitectureTestPaths.FromRoot(
            "FoodDiary.Application",
            "Dietologist",
            "Services",
            "DietologistEmailSender.cs");
        string notificationWriterPath = ArchitectureTestPaths.FromRoot(
            "FoodDiary.Application",
            "Notifications",
            "Services",
            "NotificationWriter.cs");
        string imageCleanupPath = ArchitectureTestPaths.FromRoot(
            "FoodDiary.Application",
            "Images",
            "Services",
            "ImageAssetCleanupService.cs");
        string userCleanupPath = ArchitectureTestPaths.FromRoot(
            "FoodDiary.Infrastructure",
            "Services",
            "UserCleanupService.cs");

        string emailSenderSource = File.ReadAllText(emailSenderPath);
        string dietologistEmailSenderSource = File.ReadAllText(dietologistEmailSenderPath);
        string notificationWriterSource = File.ReadAllText(notificationWriterPath);
        string imageCleanupSource = File.ReadAllText(imageCleanupPath);
        string userCleanupSource = File.ReadAllText(userCleanupPath);

        Assert.Contains("emailOutbox.EnqueueAsync", emailSenderSource, StringComparison.Ordinal);
        Assert.Contains("emailOutbox.EnqueueAsync", dietologistEmailSenderSource, StringComparison.Ordinal);
        Assert.Contains("webPushOutbox.EnqueueAsync", notificationWriterSource, StringComparison.Ordinal);
        Assert.Contains("imageObjectDeletionOutbox.EnqueueAsync", imageCleanupSource, StringComparison.Ordinal);
        Assert.DoesNotContain("imageStorageService.DeleteAsync", imageCleanupSource, StringComparison.Ordinal);
        Assert.Contains("imageObjectDeletionOutbox.EnqueueAsync", userCleanupSource, StringComparison.Ordinal);
    }
    [Fact]
    public void ArchitectureRoadmap_DocumentsDurableSideEffectDirection() {
        string roadmapPath = ArchitectureTestPaths.FromRoot(
            "docs",
            "backend",
            "ARCHITECTURE_IMPROVEMENT_ROADMAP.md");
        string source = File.ReadAllText(roadmapPath);

        Assert.Contains("Durable Side Effects", source, StringComparison.Ordinal);
        Assert.Contains("Event Taxonomy", source, StringComparison.Ordinal);
        Assert.Contains("Shared Outbox Policy", source, StringComparison.Ordinal);
        Assert.Contains("Keep JobManager Thin", source, StringComparison.Ordinal);
    }
}
