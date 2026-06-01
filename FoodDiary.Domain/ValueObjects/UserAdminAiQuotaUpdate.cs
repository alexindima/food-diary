using System.Runtime.InteropServices;

namespace FoodDiary.Domain.ValueObjects;

[StructLayout(LayoutKind.Auto)]
public readonly record struct UserAdminAiQuotaUpdate(
    long? AiInputTokenLimit = null,
    long? AiOutputTokenLimit = null) {
    public UserAiTokenLimitUpdate ToAiTokenLimitUpdate() {
        return new UserAiTokenLimitUpdate(AiInputTokenLimit, AiOutputTokenLimit);
    }
}
