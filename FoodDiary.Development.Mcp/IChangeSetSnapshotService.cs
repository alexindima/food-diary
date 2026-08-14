namespace FoodDiary.Development.Mcp;

public interface IChangeSetSnapshotService {
    Task<ChangeSetSnapshot> GetAsync(CancellationToken cancellationToken);
}
