using System.Diagnostics;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class CoveragePlatformPolicyTests {
    [PowerShellFact]
    public async Task CoverageConfiguration_PreservesUserSettingsAndSelectsFiltersByOperatingSystemAsync() {
        var start = new ProcessStartInfo("pwsh") {
            WorkingDirectory = ArchitectureTestPaths.RepositoryRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        start.ArgumentList.Add("-NoProfile");
        start.ArgumentList.Add("-File");
        start.ArgumentList.Add(ArchitectureTestPaths.FromRoot("scripts", "Test-CoveragePlatform.ps1"));
        using Process process = Process.Start(start)!;
        Task<string> output = process.StandardOutput.ReadToEndAsync();
        Task<string> error = process.StandardError.ReadToEndAsync();
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        try {
            await process.WaitForExitAsync(deadline.Token);
        } catch (OperationCanceledException) {
            process.Kill(entireProcessTree: true);
            Assert.Fail("Coverage configuration checks exceeded 60 seconds.");
        }
        Assert.True(process.ExitCode == 0, await output + await error);
    }
}
