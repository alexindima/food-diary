using System.ComponentModel.DataAnnotations;

namespace FoodDiary.Modules.RecipeCommunity.Presentation.RecipeLikes.Requests;

public sealed record SetRecipeLikeStateHttpRequest(bool? IsLiked) {
    public SetRecipeLikeStateHttpRequest() : this(IsLiked: null) { }

    [Required]
    public bool? IsLiked { get; init; } = IsLiked;
}
