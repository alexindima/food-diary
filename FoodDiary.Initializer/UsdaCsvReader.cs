namespace FoodDiary.Initializer;

internal static class UsdaCsvReader {
    public static async IAsyncEnumerable<string> ReadLinesAsync(string filePath) {
        using var reader = new StreamReader(filePath);
        await reader.ReadLineAsync().ConfigureAwait(false);
        while (await reader.ReadLineAsync().ConfigureAwait(false) is { } line) {
            if (!string.IsNullOrWhiteSpace(line)) {
                yield return line;
            }
        }
    }

    public static string[] ParseLine(string line) {
        var fields = new List<string>();
        bool inQuotes = false;
        int start = 0;

        for (int i = 0; i < line.Length; i++) {
            switch (line[i]) {
                case '"':
                    inQuotes = !inQuotes;
                    break;
                case ',' when !inQuotes:
                    fields.Add(ExtractField(line, start, i));
                    start = i + 1;
                    break;
            }
        }

        fields.Add(ExtractField(line, start, line.Length));
        return [.. fields];
    }

    public static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    private static string ExtractField(string line, int start, int end) {
        string field = line[start..end].Trim();
        if (field is ['"', _, ..] && field[^1] == '"') {
            field = field[1..^1].Replace("\"\"", "\"", StringComparison.Ordinal);
        }

        return field;
    }
}
