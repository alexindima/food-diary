using System.ComponentModel.DataAnnotations;
using FoodDiary.Application.Abstractions.Authentication.Common;

namespace FoodDiary.Presentation.Api.Features.Dietologist.Requests;

public sealed record AcceptInvitationHttpRequest(
    Guid InvitationId,
    [MaxLength(AuthenticationInputLimits.MaximumOpaqueTokenLength)] string Token);
