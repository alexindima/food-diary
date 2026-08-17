using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Cycles.Models;

public sealed record CycleConsentModel(
    Guid Id,
    CycleConsentPurpose Purpose,
    DateTime GrantedAtUtc,
    DateTime? RevokedAtUtc) {
    public bool IsActive => RevokedAtUtc is null;
}
