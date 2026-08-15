namespace FoodDiary.Development.Mcp.ChangeSets;

public interface IChangeSetSnapshotService {
    Task<ChangeSetSnapshot> GetAsync(CancellationToken cancellationToken);
}
