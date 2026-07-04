using System.Globalization;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class JobManagerGuardrailTests {
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
            "FastingNotifications",
            "ImageObjectDeletionOutbox",
            "NotificationWebPushOutbox",
            "UserLoginEventsCleanup",
        ];
        string[] expectedJobTypes = [
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
