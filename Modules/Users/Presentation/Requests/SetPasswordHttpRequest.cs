using System.ComponentModel.DataAnnotations;
using FoodDiary.Authentication.Contracts.Authentication.Common;

namespace FoodDiary.Modules.Users.Presentation.Requests;

public sealed record SetPasswordHttpRequest(
    [MaxLength(AuthenticationInputLimits.MaximumPasswordLength)] string NewPassword);
