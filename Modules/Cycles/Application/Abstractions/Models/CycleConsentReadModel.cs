using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Abstractions.Cycles.Models;

public sealed record CycleConsentReadModel(
    Guid Id,
    Guid CycleProfileId,
    CycleConsentPurpose Purpose,
    DateTime GrantedAtUtc,
    DateTime? RevokedAtUtc) {
    public bool IsActive => RevokedAtUtc is null;
}
