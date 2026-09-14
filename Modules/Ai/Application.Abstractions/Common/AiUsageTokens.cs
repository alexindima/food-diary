using System.Runtime.InteropServices;

namespace FoodDiary.Modules.Ai.Application.Abstractions.Common;

[StructLayout(LayoutKind.Auto)]
public readonly record struct AiUsageTokens(int InputTokens, int OutputTokens, int TotalTokens);
