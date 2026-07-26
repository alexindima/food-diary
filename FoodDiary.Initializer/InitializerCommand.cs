namespace FoodDiary.Initializer;

internal sealed record InitializerCommand(
    string Name,
    string? TargetMigration,
    string? ConnectionString,
    bool Force = false,
    string? RequestedBy = null,
    string? Reason = null) {
    public static InitializerCommand? Parse(string[] args) {
        if (args.Length == 0) {
            return null;
        }

        string? name = null;
        string? targetMigration = null;
        string? connectionString = null;
        bool force = false;
        string? requestedBy = null;
        string? reason = null;

        for (int index = 0; index < args.Length; index++) {
            string argument = args[index];

            if (argument is "--connection-string" or "-c") {
                index++;
                if (index >= args.Length) {
                    throw new InvalidOperationException("Missing value for --connection-string.");
                }

                connectionString = args[index];
                continue;
            }

            if (argument is "--force" or "-f") {
                force = true;
                continue;
            }

            if (argument is "--requested-by") {
                requestedBy = ReadOptionValue(args, ref index, argument);
                continue;
            }

            if (argument is "--reason") {
                reason = ReadOptionValue(args, ref index, argument);
                continue;
            }

            if (name is null) {
                name = argument;
                continue;
            }

            if (targetMigration is null) {
                targetMigration = argument;
                continue;
            }

            throw new InvalidOperationException($"Unexpected argument '{argument}'.");
        }

        return name is null ? null : new InitializerCommand(
            name,
            targetMigration,
            connectionString,
            force,
            requestedBy,
            reason);
    }

    private static string ReadOptionValue(string[] args, ref int index, string option) {
        index++;
        if (index >= args.Length) {
            throw new InvalidOperationException($"Missing value for {option}.");
        }

        return args[index];
    }
}
