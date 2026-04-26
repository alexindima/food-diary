using System.IO.Pipes;
using System.Runtime.InteropServices;

namespace FoodDiary.MailRelay.Tests.TestInfrastructure;

public sealed class RequiresDockerFactAttribute : FactAttribute {
    public RequiresDockerFactAttribute() {
        if (!DockerAvailability.IsAvailable(out var reason)) {
            Skip = reason;
        }
    }
}

internal static class DockerAvailability {
    private static readonly Lazy<DockerAvailabilityResult> CachedResult = new(CheckAvailability);

    public static bool IsAvailable(out string? reason) {
        var result = CachedResult.Value;
        reason = result.Reason;
        return result.IsAvailable;
    }

    private static DockerAvailabilityResult CheckAvailability() {
        try {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
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

    private sealed record DockerAvailabilityResult(bool IsAvailable, string? Reason);
}
