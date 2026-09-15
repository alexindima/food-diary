using FoodDiary.Modules.Cycles.Domain.Contracts.Enums;

namespace FoodDiary.Modules.Cycles.Application.Abstractions.Models;

public sealed record CycleConsentReadModel(
    Guid Id,
    Guid CycleProfileId,
    CycleConsentPurpose Purpose,
    DateTime GrantedAtUtc,
    DateTime? RevokedAtUtc) {
    public bool IsActive => RevokedAtUtc is null;
}
