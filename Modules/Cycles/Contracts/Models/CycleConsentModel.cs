using FoodDiary.Modules.Cycles.Domain.Contracts.Enums;

namespace FoodDiary.Modules.Cycles.Contracts.Models;

public sealed record CycleConsentModel(
    Guid Id,
    CycleConsentPurpose Purpose,
    DateTime GrantedAtUtc,
    DateTime? RevokedAtUtc) {
    public bool IsActive => RevokedAtUtc is null;
}
