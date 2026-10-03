namespace FoodDiary.Application.Contracts.Common.Abstractions.Persistence;

/// <summary>Transport-independent digest of an owner's operation key and normalized request.</summary>
public sealed record AtomicOperation(Guid UserId, string Key, string RequestHash, TimeSpan Retention);
