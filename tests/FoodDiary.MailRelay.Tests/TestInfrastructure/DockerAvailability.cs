using System.IO.Pipes;

namespace FoodDiary.MailRelay.Tests.TestInfrastructure;

[ExcludeFromCodeCoverage]
internal static class DockerAvailability {
    private static readonly Lazy<DockerAvailabilityResult> CachedResult = new(CheckAvailability);

    public static bool IsAvailable(out string? reason) {
        var result = CachedResult.Value;
        reason = result.Reason;
        return result.IsAvailable;
    }

    private static DockerAvailabilityResult CheckAvailability() {
        try {
            if (OperatingSystem.IsWindows()) {
                using var pipe = new NamedPipeClientStream(".", "docker_engine", PipeDirection.InOut, PipeOptions.None);
                pipe.Connect(200);
                return new DockerAvailabilityResult(true, null);
            }

            if (File.Exists("/var/run/docker.sock")) {
                return new DockerAvailabilityResult(true, null);
            }

            return new DockerAvailabilityResult(false, "Docker is not available on this machine.");
        } catch (Exception ex) {
            return new DockerAvailabilityResult(false, $"Docker is not available on this machine: {ex.Message}");
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed record DockerAvailabilityResult(bool IsAvailable, string? Reason);
}
