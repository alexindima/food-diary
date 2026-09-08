using System.ComponentModel;
using System.Diagnostics;

namespace FoodDiary.Development.Mcp.Tests;

[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class PowerShellFactAttribute : FactAttribute {
    private static readonly Lazy<bool> Available = new(IsPowerShellAvailable);

    public PowerShellFactAttribute() {
        if (!IsCi() && !Available.Value) {
            Skip = "Requires PowerShell 7+ (pwsh) on the test runner PATH. Restart the IDE after installing it.";
        }
    }

    private static bool IsCi() =>
        string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(Environment.GetEnvironmentVariable("CI"), "1", StringComparison.Ordinal) ||
        string.Equals(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"), "true", StringComparison.OrdinalIgnoreCase);

    private static bool IsPowerShellAvailable() {
        using Process process = new() {
            StartInfo = new ProcessStartInfo("pwsh") {
                UseShellExecute = false,
                CreateNoWindow = true,
            },
        };
        process.StartInfo.ArgumentList.Add("-NoLogo");
        process.StartInfo.ArgumentList.Add("-NoProfile");
        process.StartInfo.ArgumentList.Add("-NonInteractive");
        process.StartInfo.ArgumentList.Add("-Command");
        process.StartInfo.ArgumentList.Add("if ($PSVersionTable.PSVersion.Major -ge 7) { exit 0 } else { exit 42 }");
        try {
            process.Start();
        } catch (Win32Exception exception) when (exception.NativeErrorCode is 2 or 3) {
            return false;
        }
        if (!process.WaitForExit(10_000)) {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException("PowerShell prerequisite check exceeded 10 seconds.");
        }
        return process.ExitCode switch {
            0 => true,
            42 => false,
            _ => throw new InvalidOperationException("PowerShell prerequisite check failed unexpectedly."),
        };
    }
}
