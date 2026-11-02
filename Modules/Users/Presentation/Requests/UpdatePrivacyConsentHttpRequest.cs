using System.ComponentModel.DataAnnotations;

namespace FoodDiary.Modules.Users.Presentation.Requests;

public sealed record UpdatePrivacyConsentHttpRequest([Range(1, 3)] int Purpose, bool Granted, [Required, MaxLength(64)] string NoticeVersion);
