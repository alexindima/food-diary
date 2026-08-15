namespace FoodDiary.Development.Mcp.Wiki;

public interface IWikiCommandExecutor {
    Task<WikiCommandResult> ExecuteAsync(
        string command,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken);
}
