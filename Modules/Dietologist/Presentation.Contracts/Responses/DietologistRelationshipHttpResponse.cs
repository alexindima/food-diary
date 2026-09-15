namespace FoodDiary.Modules.Dietologist.Presentation.Contracts.Responses;

public sealed record DietologistRelationshipHttpResponse(
    Guid InvitationId,
    string Status,
    string? Email,
    string? FirstName,
    string? LastName,
    Guid? DietologistUserId,
    DietologistPermissionsHttpResponse Permissions,
    DateTime CreatedAtUtc,
    DateTime ExpiresAtUtc,
    DateTime? AcceptedAtUtc);
