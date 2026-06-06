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
            if (line[i] == '"') {
                inQuotes = !inQuotes;
            } else if (line[i] == ',' && !inQuotes) {
                fields.Add(ExtractField(line, start, i));
                start = i + 1;
            }
        }

        fields.Add(ExtractField(line, start, line.Length));
        return fields.ToArray();
    }

    public static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    private static string ExtractField(string line, int start, int end) {
        string field = line[start..end].Trim();
        if (field.Length >= 2 && field[0] == '"' && field[^1] == '"') {
            field = field[1..^1].Replace("\"\"", "\"");
        }

        return field;
    }
}
