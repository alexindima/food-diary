using System.IO.Pipes;

namespace FoodDiary.Infrastructure.Tests.Integration;

[ExcludeFromCodeCoverage]
internal static class DockerAvailability {
    private static readonly Lazy<DockerAvailabilityResult> CachedResult = new(CheckAvailability);

    public static bool IsAvailable(out string? reason) {
        DockerAvailabilityResult result = CachedResult.Value;
        reason = result.Reason;
        return result.IsAvailable;
    }

    private static DockerAvailabilityResult CheckAvailability() {
        try {
            if (OperatingSystem.IsWindows()) {
                using var pipe = new NamedPipeClientStream(".", "docker_engine", PipeDirection.InOut, PipeOptions.None);
                pipe.Connect(200);
                return new DockerAvailabilityResult(IsAvailable: true, Reason: null);
            }

            if (File.Exists("/var/run/docker.sock")) {
                return new DockerAvailabilityResult(IsAvailable: true, Reason: null);
            }

            return new DockerAvailabilityResult(IsAvailable: false, "Docker is not available on this machine.");
        } catch (Exception ex) {
            return new DockerAvailabilityResult(IsAvailable: false, $"Docker is not available on this machine: {ex.Message}");
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed record DockerAvailabilityResult(bool IsAvailable, string? Reason);
}
