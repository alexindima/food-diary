namespace FoodDiary.Modules.Dietologist.Application.Abstractions.Common;

public interface IDietologistEmailSender {
    Task SendDietologistInvitationAsync(DietologistInvitationMessage message, CancellationToken cancellationToken = default);
}
