using System.Globalization;
using Xunit;

namespace FoodDiary.Testing;

[AttributeUsage(AttributeTargets.Method)]
[ExcludeFromCodeCoverage]
public sealed class RequiresDockerPerformanceFactAttribute : FactAttribute {
    public RequiresDockerPerformanceFactAttribute() {
        if (!DockerAvailability.IsAvailable(out string? reason)) {
            Skip = reason;
        } else if (IsProfilingEnabled("CORECLR_ENABLE_PROFILING") || IsProfilingEnabled("COR_ENABLE_PROFILING")) {
            Skip = "Latency budgets require an unprofiled run. Run this test without coverage or a profiler.";
        }
    }

    private static bool IsProfilingEnabled(string variable) =>
        int.TryParse(Environment.GetEnvironmentVariable(variable), CultureInfo.InvariantCulture, out int enabled) && enabled != 0;
}
