namespace FoodDiary.Application.Dietologist.Common;

public interface IDietologistEmailSender {
    Task SendDietologistInvitationAsync(DietologistInvitationMessage message, CancellationToken cancellationToken = default);
}

public sealed record DietologistInvitationMessage(
    string ToEmail,
    Guid InvitationId,
    string Token,
    string? ClientFirstName,
    string? ClientLastName,
    string? Language);
