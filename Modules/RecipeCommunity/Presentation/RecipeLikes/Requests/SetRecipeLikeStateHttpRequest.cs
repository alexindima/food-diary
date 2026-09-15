using System.ComponentModel.DataAnnotations;

namespace FoodDiary.Modules.RecipeCommunity.Presentation.RecipeLikes.Requests;

public sealed record SetRecipeLikeStateHttpRequest([property: Required] bool? IsLiked);
