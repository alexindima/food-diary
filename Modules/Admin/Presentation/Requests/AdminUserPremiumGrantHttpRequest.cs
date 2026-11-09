using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FoodDiary.Modules.Admin.Presentation.Requests;

public sealed record AdminUserPremiumGrantHttpRequest(
    [property: JsonRequired] DateTimeOffset EndsAtUtc,
    [property: JsonRequired]
    [Required] string Reason);
