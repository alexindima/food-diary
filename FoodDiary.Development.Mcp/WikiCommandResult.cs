namespace FoodDiary.Development.Mcp;

public sealed record WikiCommandResult(
    string Command,
    string Output,
    string RepositoryRoot,
    bool ReadOnly = true);
