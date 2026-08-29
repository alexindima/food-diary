using Microsoft.AspNetCore.Mvc.ModelBinding;
using FoodDiary.Presentation.Api.Extensions;

namespace FoodDiary.Presentation.Api.Controllers;

public sealed class CurrentRefreshSessionIdModelBinder : IModelBinder {
    public Task BindModelAsync(ModelBindingContext bindingContext) {
        ArgumentNullException.ThrowIfNull(bindingContext);
        Guid? sessionId = bindingContext.HttpContext.User.GetRefreshSessionGuid();
        if (bindingContext.ModelType != typeof(Guid) || !sessionId.HasValue) {
            throw new CurrentUserUnavailableException();
        }

        bindingContext.Result = ModelBindingResult.Success(sessionId.Value);
        return Task.CompletedTask;
    }
}
