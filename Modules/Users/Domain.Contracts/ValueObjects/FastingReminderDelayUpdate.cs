using System.Runtime.InteropServices;

namespace FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;

/// <summary>Partial elapsed-delay input; the owner merges it before validating the pair.</summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct FastingReminderDelayUpdate(int? FirstHours = null, int? FollowUpHours = null);
