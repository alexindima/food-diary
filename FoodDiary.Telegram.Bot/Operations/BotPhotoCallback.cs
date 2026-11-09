using Telegram.Bot.Types.ReplyMarkups;

namespace FoodDiary.Telegram.Bot.Operations;

internal readonly record struct BotPhotoCallback(BotOperationId OperationId, bool StartsTrial) {
    internal const string TrialPrefix = "photo-trial:";
    private const string ContinuePrefix = "photo-continue:";
    internal const string WaitingForConsent = "waiting-for-photo-consent";
    internal const string ConsentCheckpoint = "{\"Stage\":\"waiting-for-photo-consent\"}";

    internal string ExpectedCheckpoint => StartsTrial ? BotPhotoTrial.OfferedCheckpoint : ConsentCheckpoint;
    internal string NextCheckpoint => StartsTrial ? BotPhotoTrial.AcceptedCheckpoint : "{\"Stage\":\"received\"}";

    internal static bool TryRead(string? data, out BotPhotoCallback callback) {
        callback = default;
        bool startsTrial = data?.StartsWith(TrialPrefix, StringComparison.Ordinal) == true;
        if (data is null || (!startsTrial && !data.StartsWith(ContinuePrefix, StringComparison.Ordinal))) {
            return false;
        }
        string prefix = startsTrial ? TrialPrefix : ContinuePrefix;
        if (!Guid.TryParseExact(data.AsSpan(prefix.Length), "N", out Guid id) || id == Guid.Empty) {
            return false;
        }
        callback = new BotPhotoCallback(new BotOperationId(id), startsTrial);
        return true;
    }

    internal static InlineKeyboardButton ContinueButton(BotOperationId operationId, bool russian) =>
        InlineKeyboardButton.WithCallbackData(russian ? "Продолжить распознавание" : "Continue recognition",
            $"{ContinuePrefix}{operationId.Value:N}");
}
