namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.TransferTelegramDiary;

internal sealed record TelegramDiaryTransferAttempt(Guid SourceId, long SourceSecurityVersion, Guid TargetId,
    long TargetSecurityVersion, string Email) {
    internal const string Purpose = "telegram-diary-transfer";
}
