using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Presentation.Api.Extensions;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FoodDiary.Presentation.Api.Controllers;

public sealed class CurrentUserIdModelBinder : IModelBinder {
    public const string UnauthorizedItemKey = "__current_user_unauthorized";

    public Task BindModelAsync(ModelBindingContext bindingContext) {
        ArgumentNullException.ThrowIfNull(bindingContext);

        if (bindingContext.ModelType != typeof(UserId) && bindingContext.ModelType != typeof(Guid)) {
            bindingContext.Result = ModelBindingResult.Failed();
            return Task.CompletedTask;
        }

        var userId = bindingContext.HttpContext.User.GetUserGuid();
        if (!userId.HasValue || userId.Value == Guid.Empty) {
            bindingContext.HttpContext.Items[UnauthorizedItemKey] = true;
            bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, "Current user is not available.");
            bindingContext.Result = ModelBindingResult.Failed();
            return Task.CompletedTask;
        }

        bindingContext.Result = bindingContext.ModelType == typeof(Guid)
            ? ModelBindingResult.Success(userId.Value)
            : ModelBindingResult.Success(new UserId(userId.Value));
        return Task.CompletedTask;
    }
}
