using System.ComponentModel.DataAnnotations;
using FoodDiary.Authentication.Contracts.Authentication.Common;

namespace FoodDiary.Modules.Users.Presentation.Requests;

public sealed record ChangePasswordHttpRequest(
    [MaxLength(AuthenticationInputLimits.MaximumPasswordLength)] string CurrentPassword,
    [MaxLength(AuthenticationInputLimits.MaximumPasswordLength)] string NewPassword);
