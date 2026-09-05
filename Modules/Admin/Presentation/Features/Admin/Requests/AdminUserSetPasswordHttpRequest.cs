using System.ComponentModel.DataAnnotations;
using FoodDiary.Application.Abstractions.Authentication.Common;

namespace FoodDiary.Presentation.Api.Features.Admin.Requests;

public sealed record AdminUserSetPasswordHttpRequest(
    [MaxLength(AuthenticationInputLimits.MaximumPasswordLength)] string NewPassword);
