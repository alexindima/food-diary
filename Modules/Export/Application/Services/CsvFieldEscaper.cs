namespace FoodDiary.Application.Export.Services;

internal static class CsvFieldEscaper {
    public static string Escape(string? value) {
        if (string.IsNullOrEmpty(value)) {
            return "";
        }

        string safeValue = NeutralizeSpreadsheetFormula(value);
        ReadOnlySpan<char> valueSpan = safeValue.AsSpan();
        if (valueSpan.Contains('"') ||
            valueSpan.Contains(',') ||
            valueSpan.Contains('\n') ||
            valueSpan.Contains('\r')) {
            return $"\"{safeValue.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
        }

        return safeValue;
    }

    private static string NeutralizeSpreadsheetFormula(string value) {
        int firstMeaningfulIndex = 0;
        while (firstMeaningfulIndex < value.Length &&
               (char.IsWhiteSpace(value[firstMeaningfulIndex]) || char.IsControl(value[firstMeaningfulIndex]))) {
            firstMeaningfulIndex++;
        }

        return firstMeaningfulIndex < value.Length && value[firstMeaningfulIndex] is '=' or '+' or '-' or '@'
            ? $"'{value}"
            : value;
    }
}
