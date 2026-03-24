using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FoodDiary.Presentation.Api.Controllers;

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class FromCurrentUserAttribute() : ModelBinderAttribute(typeof(CurrentUserIdModelBinder)) {
    public override BindingSource BindingSource => BindingSource.Custom;
}
