namespace FoodDiary.Application.Abstractions.Dietologist.Common;

public sealed record DietologistInvitationMessage(
    string ToEmail,
    Guid InvitationId,
    string Token,
    string? ClientFirstName,
    string? ClientLastName,
    string? Language);
