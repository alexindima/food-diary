namespace FoodDiary.Application.Abstractions.Dietologist.Common;

public interface IDietologistEmailSender {
    Task SendDietologistInvitationAsync(DietologistInvitationMessage message, CancellationToken cancellationToken = default);
}
