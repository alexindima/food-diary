using FoodDiary.Presentation.Api.Extensions;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FoodDiary.Presentation.Api.Controllers;

public sealed class CurrentUserIdModelBinder : IModelBinder {
    public Task BindModelAsync(ModelBindingContext bindingContext) {
        ArgumentNullException.ThrowIfNull(bindingContext);

        if (bindingContext.ModelType != typeof(Guid)) {
            bindingContext.Result = ModelBindingResult.Failed();
            return Task.CompletedTask;
        }

        var userId = bindingContext.HttpContext.User.GetUserGuid();
        if (!userId.HasValue || userId.Value == Guid.Empty) {
            throw new CurrentUserUnavailableException();
        }

        bindingContext.Result = ModelBindingResult.Success(userId.Value);
        return Task.CompletedTask;
    }
}
