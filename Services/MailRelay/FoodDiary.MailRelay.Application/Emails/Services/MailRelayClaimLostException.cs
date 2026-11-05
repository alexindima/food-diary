namespace FoodDiary.MailRelay.Application.Emails.Services;

public sealed class MailRelayClaimLostException()
    : Exception("The email delivery claim is no longer owned by this attempt.");
