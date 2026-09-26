using System.ComponentModel.DataAnnotations;
using FoodDiary.Authentication.Contracts.Authentication.Common;

namespace FoodDiary.Modules.Admin.Presentation.Requests;

public sealed record AdminUserSetPasswordHttpRequest(
    [MaxLength(AuthenticationInputLimits.MaximumPasswordLength)] string NewPassword);
