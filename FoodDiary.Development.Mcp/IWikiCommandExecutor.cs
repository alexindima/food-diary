namespace FoodDiary.Development.Mcp;

public interface IWikiCommandExecutor {
    Task<WikiCommandResult> ExecuteAsync(
        string command,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken);
}
