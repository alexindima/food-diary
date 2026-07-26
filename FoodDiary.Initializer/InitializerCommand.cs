namespace FoodDiary.Initializer;

internal sealed record InitializerCommand(
    string Name,
    string? TargetMigration,
    string? ConnectionString,
    bool Force = false,
    string? RequestedBy = null,
    string? Reason = null,
    bool DryRun = false,
    int Limit = 50,
    int? ExpectedAttemptCount = null) {
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
        bool dryRun = false;
        int limit = 50;
        int? expectedAttemptCount = null;

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

            if (argument is "--dry-run") {
                dryRun = true;
                continue;
            }

            if (argument is "--limit") {
                limit = ReadPositiveIntOption(args, ref index, argument);
                continue;
            }

            if (argument is "--expected-attempt-count") {
                expectedAttemptCount = ReadPositiveIntOption(args, ref index, argument);
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
            reason,
            dryRun,
            limit,
            expectedAttemptCount);
    }

    private static string ReadOptionValue(string[] args, ref int index, string option) {
        index++;
        if (index >= args.Length) {
            throw new InvalidOperationException($"Missing value for {option}.");
        }

        return args[index];
    }

    private static int ReadPositiveIntOption(string[] args, ref int index, string option) {
        string value = ReadOptionValue(args, ref index, option);
        if (!int.TryParse(
                value,
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture,
                out int result) ||
            result <= 0) {
            throw new InvalidOperationException($"{option} requires a positive integer.");
        }

        return result;
    }
}
