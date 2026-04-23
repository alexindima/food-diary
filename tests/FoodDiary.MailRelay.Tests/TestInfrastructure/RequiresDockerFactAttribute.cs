using System.Net.Sockets;

namespace FoodDiary.MailRelay.Tests.TestInfrastructure;

public sealed class RequiresDockerFactAttribute : FactAttribute {
    public RequiresDockerFactAttribute() {
        if (IsDockerUnavailable()) {
            Skip = "Docker is unavailable for MailRelay integration tests.";
        }
    }

    private static bool IsDockerUnavailable() {
        try {
            using var client = new TcpClient();
            client.Connect("127.0.0.1", 2375);
            return false;
        } catch {
            return Environment.GetEnvironmentVariable("DOCKER_HOST") is null &&
                   !File.Exists(@"\\.\pipe\docker_engine");
        }
    }
}
