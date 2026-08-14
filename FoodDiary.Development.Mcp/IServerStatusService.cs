namespace FoodDiary.Development.Mcp;

public interface IServerStatusService {
    Task<ServerStatus> GetStatusAsync(CancellationToken cancellationToken);
}
