namespace FoodDiary.Development.Mcp.ChangeSets;

public interface IChangeSetSnapshotService {
    Task<ChangeSetSnapshot> GetAsync(CancellationToken cancellationToken);

    Task<ChangeSetSnapshot> GetAsync(
        IReadOnlyList<string>? relevantPaths,
        CancellationToken cancellationToken);

    Task<ChangeSetSnapshot> RefreshAsync(CancellationToken cancellationToken);

    Task<ChangeSetSnapshot> RefreshAsync(
        IReadOnlyList<string>? relevantPaths,
        CancellationToken cancellationToken);
}
