using System.Runtime.InteropServices;

namespace FoodDiary.Application.Abstractions.Ai.Common;

[StructLayout(LayoutKind.Auto)]
public readonly record struct AiUsageTokens(int InputTokens, int OutputTokens, int TotalTokens);
