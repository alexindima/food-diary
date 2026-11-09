using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace FoodDiary.Telegram.Bot;

[ExcludeFromCodeCoverage]
internal static class BotInputParser {
    private const string WaterPrefix = "water:";

    internal static bool TryParseCustomWaterAmount(string? text, out int amountMl) =>
        int.TryParse(text.AsSpan().Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out amountMl) && amountMl is > 0 and <= 10_000;

    internal static bool TryParseWaterAmount(string? callbackData, out int amountMl) {
        amountMl = 0;
        if (string.IsNullOrWhiteSpace(callbackData) ||
            !callbackData.StartsWith(WaterPrefix, StringComparison.Ordinal)) {
            return false;
        }

        string amountText = callbackData[WaterPrefix.Length..];
        if (int.TryParse(amountText, NumberStyles.Integer, CultureInfo.InvariantCulture, out amountMl) && amountMl > 0) {
            return true;
        }

        amountMl = 0;
        return false;
    }
}
