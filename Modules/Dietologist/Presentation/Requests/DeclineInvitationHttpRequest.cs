using System.ComponentModel.DataAnnotations;
using FoodDiary.Authentication.Contracts.Authentication.Common;

namespace FoodDiary.Modules.Dietologist.Presentation.Requests;

public sealed record DeclineInvitationHttpRequest(
    Guid InvitationId,
    [MaxLength(AuthenticationInputLimits.MaximumOpaqueTokenLength)] string Token);
