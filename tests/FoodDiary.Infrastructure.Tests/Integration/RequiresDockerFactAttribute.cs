using System.IO.Pipes;
using System.Runtime.InteropServices;

namespace FoodDiary.Infrastructure.Tests.Integration;

[AttributeUsage(AttributeTargets.Method)]
public sealed class RequiresDockerFactAttribute : FactAttribute {
    public RequiresDockerFactAttribute() {
        if (!DockerAvailability.IsAvailable(out var reason)) {
            Skip = reason;
        }
    }
}

internal static class DockerAvailability {
    public static bool IsAvailable(out string? reason) {
        try {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                using var pipe = new NamedPipeClientStream(".", "docker_engine", PipeDirection.InOut, PipeOptions.None);
                pipe.Connect(200);
                reason = null;
                return true;
            }

            if (File.Exists("/var/run/docker.sock")) {
                reason = null;
                return true;
            }

            reason = "Docker is not available on this machine.";
            return false;
        } catch (Exception ex) {
            reason = $"Docker is not available on this machine: {ex.Message}";
            return false;
        }
    }
}
