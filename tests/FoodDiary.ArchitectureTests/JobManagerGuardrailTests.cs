using System.Globalization;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class JobManagerGuardrailTests {
    [Fact]
    public void JobManagerProject_ReferencesOnlyApprovedRuntimeModulesAndSchedulerPackages() {
        const string relativeProjectPath = "FoodDiary.JobManager/FoodDiary.JobManager.csproj";
        string[] expectedProjectReferences = [
            "FoodDiary.Application",
            "FoodDiary.Infrastructure",
            "FoodDiary.Integrations",
            "FoodDiary.Resources",
        ];
        string[] expectedPackageReferences = [
            "Hangfire.AspNetCore",
            "Hangfire.Core",
            "Hangfire.PostgreSql",
            "Microsoft.EntityFrameworkCore",
            "Microsoft.EntityFrameworkCore.Relational",
            "Microsoft.Extensions.Hosting",
            "Newtonsoft.Json",
        ];

        string[] projectReferences = ProjectReferenceReader.ReadProjectReferences(relativeProjectPath);
        string[] packageReferences = ProjectReferenceReader.ReadPackageReferences(relativeProjectPath);

        Assert.Equal(expectedProjectReferences, projectReferences);
        Assert.Equal(expectedPackageReferences, packageReferences);
    }

    [Fact]
    public void JobManagerRootFolders_StayLimitedToWorkerHostStructure() {
        string jobManagerRoot = ArchitectureTestPaths.FromRoot("FoodDiary.JobManager");
        string[] allowedDirectories = [
            "Services",
        ];

        string[] unexpectedDirectories = [.. Directory.GetDirectories(jobManagerRoot)
            .Select(Path.GetFileName)
            .Where(name => name is not null)
            .Select(name => name!)
            .Where(name => !name.Equals("bin", StringComparison.OrdinalIgnoreCase))
            .Where(name => !name.Equals("obj", StringComparison.OrdinalIgnoreCase))
            .Where(name => !allowedDirectories.Contains(name, StringComparer.Ordinal))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(unexpectedDirectories);
    }

    [Fact]
    public void JobManagerSource_DoesNotReferenceHttpPresentationOrHostApiSurface() {
        string jobManagerRoot = ArchitectureTestPaths.FromRoot("FoodDiary.JobManager");

        string[] violations = SourceScanner.FindLinePatternViolations(jobManagerRoot, [
            "FoodDiary.Presentation.Api",
            "FoodDiary.Web.Api",
            "Microsoft.AspNetCore.Mvc",
            "ControllerBase",
            "IActionResult",
            "HttpContext",
            "MapGet(",
            "MapPost(",
            "MapPut(",
            "MapPatch(",
            "MapDelete(",
        ]);

        Assert.Empty(violations);
    }

    [Fact]
    public void JobManagerProductionCode_UsesCancellationTokenNoneOnlyForHangfireRegistrationPlaceholders() {
        string root = ArchitectureTestPaths.RepositoryRoot;
        string jobManagerRoot = ArchitectureTestPaths.FromRoot("FoodDiary.JobManager");

        string[] violations = [.. SourceScanner.SourceFiles(jobManagerRoot)
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { path, index, line }))
            .Where(static entry => entry.line.Contains("CancellationToken.None", StringComparison.Ordinal))
            .Where(static entry => !entry.line.Contains("Job.FromExpression", StringComparison.Ordinal))
            .Select(entry => string.Create(
                CultureInfo.InvariantCulture,
                $"{Path.GetRelativePath(root, entry.path)}:{entry.index + 1}"))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void JobManagerSource_DoesNotOwnPersistenceOrMediatorWorkflows() {
        string jobManagerRoot = ArchitectureTestPaths.FromRoot("FoodDiary.JobManager");

        string[] violations = SourceScanner.FindLinePatternViolations(jobManagerRoot, [
            "FoodDiary.Infrastructure.Persistence",
            "DbContext",
            "IUnitOfWork",
            "SaveChangesAsync(",
            "ISender",
            "IMediator",
            ".Send(",
            ".Publish(",
        ]);

        Assert.Empty(violations);
    }

    [Fact]
    public void RecurringJobIds_AllIncludesEveryRecurringJobIdConstant() {
        string recurringJobIdsPath = ArchitectureTestPaths.FromRoot(
            "FoodDiary.JobManager",
            "Services",
            "RecurringJobIds.cs");
        string source = File.ReadAllText(recurringJobIdsPath);
        string allInitializer = source[source.IndexOf("public static readonly string[] All", StringComparison.Ordinal)..];

        string[] constants = [.. File.ReadLines(recurringJobIdsPath)
            .Select(static line => line.Trim())
            .Where(static line => line.StartsWith("public const string ", StringComparison.Ordinal))
            .Select(static line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries)[3])];

        string[] missing = [.. constants
            .Where(constantName => !allInitializer.Contains(constantName, StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(missing);
    }

    [Fact]
    public void MigratedJobManagerJobs_UseSharedExecutionObserver() {
        string root = ArchitectureTestPaths.RepositoryRoot;
        string servicesRoot = ArchitectureTestPaths.FromRoot("FoodDiary.JobManager", "Services");
        string[] migratedJobFiles = [
            Path.Combine(servicesRoot, "BillingRenewalJob.cs"),
            Path.Combine(servicesRoot, "EmailOutboxJob.cs"),
            Path.Combine(servicesRoot, "FastingNotificationJob.cs"),
            Path.Combine(servicesRoot, "ImageCleanupJob.cs"),
            Path.Combine(servicesRoot, "ImageObjectDeletionOutboxJob.cs"),
            Path.Combine(servicesRoot, "NotificationCleanupJob.cs"),
            Path.Combine(servicesRoot, "NotificationWebPushOutboxJob.cs"),
            Path.Combine(servicesRoot, "UserCleanupJob.cs"),
            Path.Combine(servicesRoot, "UserLoginEventCleanupJob.cs"),
        ];

        string[] violations = [.. migratedJobFiles
            .Where(path => {
                string source = File.ReadAllText(path);
                return !source.Contains("JobExecutionObserver observer", StringComparison.Ordinal) ||
                       !source.Contains("observer.Start(JobName)", StringComparison.Ordinal) ||
                       !source.Contains("observer.RecordSuccess", StringComparison.Ordinal) ||
                       !source.Contains("observer.RecordFailure", StringComparison.Ordinal) ||
                       !source.Contains("observer.RecordCanceled", StringComparison.Ordinal) ||
                       !source.Contains("JobExecutionObserver.RecordDuration", StringComparison.Ordinal);
            })
            .Select(path => Path.GetRelativePath(root, path))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }
    [Fact]
    public void JobManager_RegistersBackgroundWorkMigratedOutOfPrimaryApiHost() {
        string recurringJobIdsPath = ArchitectureTestPaths.FromRoot(
            "FoodDiary.JobManager",
            "Services",
            "RecurringJobIds.cs");
        string recurringJobsHostedServicePath = ArchitectureTestPaths.FromRoot(
            "FoodDiary.JobManager",
            "Services",
            "RecurringJobsHostedService.cs");
        string recurringJobIdsSource = File.ReadAllText(recurringJobIdsPath);
        string recurringJobsHostedServiceSource = File.ReadAllText(recurringJobsHostedServicePath);
        string[] expectedJobIds = [
            "BillingRenewal",
            "EmailOutbox",
            "FastingNotifications",
            "ImageObjectDeletionOutbox",
            "NotificationWebPushOutbox",
            "UserLoginEventsCleanup",
        ];
        string[] expectedJobTypes = [
            "BillingRenewalJob",
            "EmailOutboxJob",
            "FastingNotificationJob",
            "ImageObjectDeletionOutboxJob",
            "NotificationWebPushOutboxJob",
            "UserLoginEventCleanupJob",
        ];

        foreach (string expectedJobId in expectedJobIds) {
            Assert.Contains(expectedJobId, recurringJobIdsSource, StringComparison.Ordinal);
            Assert.Contains($"RecurringJobIds.{expectedJobId}", recurringJobsHostedServiceSource, StringComparison.Ordinal);
        }

        foreach (string expectedJobType in expectedJobTypes) {
            Assert.Contains($"Job.FromExpression<{expectedJobType}>", recurringJobsHostedServiceSource, StringComparison.Ordinal);
        }
    }
}
