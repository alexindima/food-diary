using System.Diagnostics;
using System.Globalization;

namespace FoodDiary.Development.Mcp.Tests;

public sealed partial class NodeTaskProcessBoundaryTests {
    [WindowsFileSharingFact]
    public async Task PublishedIdentity_ExclusiveFileLock_IsRetriedWithinTheExistingDeadline() {
        using var repository = new NodeRepository(script: null);
        string identityPath = Path.Combine(repository.Root, "parent.pid");
        await File.WriteAllTextAsync(identityPath, Environment.ProcessId.ToString(CultureInfo.InvariantCulture), CancellationToken.None);

        Task<Process> reading;
        await using (var publication = new FileStream(identityPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) {
            reading = ReadOwnedProcessAsync(identityPath);
            Assert.False(reading.IsCompleted, "The identity reader must wait while the published file is locked.");
        }

        using Process process = await reading.WaitAsync(TestWaitTimeout);
        Assert.Equal(Environment.ProcessId, process.Id);
    }

    [ExcludeFromCodeCoverage]
    [AttributeUsage(AttributeTargets.Method)]
    private sealed class WindowsFileSharingFactAttribute : FactAttribute {
        public WindowsFileSharingFactAttribute() {
            if (!OperatingSystem.IsWindows()) {
                Skip = "Requires Windows file-sharing locks.";
            }
        }
    }
}
