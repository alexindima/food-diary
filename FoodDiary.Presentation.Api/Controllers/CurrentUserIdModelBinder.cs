using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Presentation.Api.Extensions;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FoodDiary.Presentation.Api.Controllers;

public sealed class CurrentUserIdModelBinder : IModelBinder {
    public const string UnauthorizedItemKey = "__current_user_unauthorized";

    public Task BindModelAsync(ModelBindingContext bindingContext) {
        ArgumentNullException.ThrowIfNull(bindingContext);

        if (bindingContext.ModelType != typeof(UserId)) {
            bindingContext.Result = ModelBindingResult.Failed();
            return Task.CompletedTask;
        }

        var userId = bindingContext.HttpContext.User.GetUserId();
        if (userId is null || userId.Value == UserId.Empty) {
            bindingContext.HttpContext.Items[UnauthorizedItemKey] = true;
            bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, "Current user is not available.");
            bindingContext.Result = ModelBindingResult.Failed();
            return Task.CompletedTask;
        }

        bindingContext.Result = ModelBindingResult.Success(userId.Value);
        return Task.CompletedTask;
    }
}
