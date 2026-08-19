using System.ComponentModel.DataAnnotations;

namespace FoodDiary.Presentation.Api.Features.RecipeLikes.Requests;

public sealed record SetRecipeLikeStateHttpRequest([property: Required] bool? IsLiked);
