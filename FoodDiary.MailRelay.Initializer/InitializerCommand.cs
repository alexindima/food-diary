namespace FoodDiary.MailRelay.Initializer;

internal sealed record InitializerCommand(string Name, string? ConnectionString) {
    public static InitializerCommand? Parse(string[] args) {
        if (args.Length == 0) {
            return null;
        }

        string? name = null;
        string? connectionString = null;

        for (var index = 0; index < args.Length; index++) {
            var argument = args[index];

            if (argument is "--connection-string" or "-c") {
                index++;
                if (index >= args.Length) {
                    throw new InvalidOperationException("Missing value for --connection-string.");
                }

                connectionString = args[index];
                continue;
            }

            if (name is null) {
                name = argument;
                continue;
            }

            throw new InvalidOperationException($"Unexpected argument '{argument}'.");
        }

        return name is null ? null : new InitializerCommand(name, connectionString);
    }
}
