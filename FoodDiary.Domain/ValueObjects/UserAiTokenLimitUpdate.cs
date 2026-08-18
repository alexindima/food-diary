using System.Runtime.InteropServices;

namespace FoodDiary.Domain.ValueObjects;

[StructLayout(LayoutKind.Auto)]
public readonly record struct UserAiTokenLimitUpdate(
    long? InputLimit = null,
    long? OutputLimit = null);
