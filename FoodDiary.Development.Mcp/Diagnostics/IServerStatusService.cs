namespace FoodDiary.Development.Mcp.Diagnostics;

public interface IServerStatusService {
    Task<ServerStatus> GetStatusAsync(CancellationToken cancellationToken);
}
