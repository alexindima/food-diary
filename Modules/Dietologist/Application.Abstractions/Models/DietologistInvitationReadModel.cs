using FoodDiary.Modules.Dietologist.Domain.Enums;
using FoodDiary.Modules.Dietologist.Contracts.Models;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Modules.Dietologist.Application.Abstractions.Models;

public sealed record DietologistInvitationReadModel(
    Guid InvitationId,
    Guid ClientUserId,
    Guid? DietologistUserId,
    string DietologistEmail,
    string? ClientEmail,
    string? ClientFirstName,
    string? ClientLastName,
    string? ClientProfileImage,
    DateTime? ClientBirthDate,
    string? ClientGender,
    double? ClientHeightCm,
    ActivityLevel ClientActivityLevel,
    string? DietologistUserEmail,
    string? DietologistFirstName,
    string? DietologistLastName,
    DietologistInvitationStatus Status,
    DietologistPermissionsReadModel Permissions,
    DateTime CreatedAtUtc,
    DateTime ExpiresAtUtc,
    DateTime? AcceptedAtUtc);
