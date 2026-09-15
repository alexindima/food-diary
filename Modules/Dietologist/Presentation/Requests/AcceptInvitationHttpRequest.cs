using System.ComponentModel.DataAnnotations;
using FoodDiary.Application.Abstractions.Authentication.Common;

namespace FoodDiary.Modules.Dietologist.Presentation.Requests;

public sealed record AcceptInvitationHttpRequest(
    Guid InvitationId,
    [MaxLength(AuthenticationInputLimits.MaximumOpaqueTokenLength)] string Token);
