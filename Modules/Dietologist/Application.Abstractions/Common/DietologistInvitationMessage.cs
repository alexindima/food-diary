namespace FoodDiary.Modules.Dietologist.Application.Abstractions.Common;

public sealed record DietologistInvitationMessage(
    string ToEmail,
    Guid InvitationId,
    string Token,
    string? ClientFirstName,
    string? ClientLastName,
    string? Language);
