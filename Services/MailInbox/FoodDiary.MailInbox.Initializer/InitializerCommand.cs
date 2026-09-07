namespace FoodDiary.MailInbox.Initializer;

internal sealed record InitializerCommand(string Name) {
    public static InitializerCommand? Parse(string[] args) {
        if (args.Length == 0) {
            return null;
        }

        string? name = null;

        for (int index = 0; index < args.Length; index++) {
            string argument = args[index];

            if (name is not null) {
                throw new InvalidOperationException("Unexpected argument.");
            }

            name = argument;
        }

        return name is null ? null : new InitializerCommand(name);
    }
}
