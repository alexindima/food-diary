namespace FoodDiary.MailInbox.Initializer;

internal sealed record InitializerCommand(string Name, string? ConnectionString) {
    public static InitializerCommand? Parse(string[] args) {
        if (args.Length == 0) {
            return null;
        }

        string? name = null;
        string? connectionString = null;

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

            if (name is not null) {
                throw new InvalidOperationException($"Unexpected argument '{argument}'.");
            }

            name = argument;
        }

        return name is null ? null : new InitializerCommand(name, connectionString);
    }
}
