using System.Text.Json;
using FoodDiary.Modules.RecipeCommunity.Presentation.RecipeLikes.Requests;
using FoodDiary.Modules.Wearables.Presentation.Requests;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class PositionalRequestValidationTests {
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);
    [Theory]
    [InlineData("{\"isLiked\":true}", true)]
    [InlineData("{\"isLiked\":false}", true)]
    [InlineData("{}", false)]
    [InlineData("{\"isLiked\":null}", false)]
    public void LikeState_UsesMvcPropertyValidation(string json, bool valid) {
        SetRecipeLikeStateHttpRequest request = JsonSerializer.Deserialize<SetRecipeLikeStateHttpRequest>(json, WebJsonOptions)!;
        ValidateWithMvc(request, valid);
    }

    [Theory]
    [InlineData("{\"code\":\"authorization-code\",\"state\":\"protected-state\"}", true)]
    [InlineData("{}", false)]
    [InlineData("{\"code\":\"authorization-code\"}", false)]
    [InlineData("{\"state\":\"protected-state\"}", false)]
    [InlineData("{\"code\":\"\",\"state\":\"protected-state\"}", false)]
    [InlineData("{\"code\":\"authorization-code\",\"state\":\"\"}", false)]
    public void WearableConnection_UsesMvcPropertyValidation(string json, bool valid) {
        ConnectWearableHttpRequest request = JsonSerializer.Deserialize<ConnectWearableHttpRequest>(json, WebJsonOptions)!;
        ValidateWithMvc(request, valid);
    }

    [Fact]
    public void WearableConnection_RejectsOversizedCodeAndStateInMvc() {
        ValidateWithMvc(new ConnectWearableHttpRequest(new string('x', 20_000), "state"), valid: false);
        ValidateWithMvc(new ConnectWearableHttpRequest("code", new string('x', 20_000)), valid: false);
    }

    private static void ValidateWithMvc(object request, bool valid) {
        using ServiceProvider services = new ServiceCollection().AddLogging().AddControllers().Services.BuildServiceProvider();
        var context = new ActionContext(new DefaultHttpContext { RequestServices = services }, new RouteData(), new ActionDescriptor(), new ModelStateDictionary());
        services.GetRequiredService<IObjectModelValidator>().Validate(context, validationState: null, prefix: string.Empty, request);
        Assert.Equal(valid, context.ModelState.IsValid);
        if (!valid) {
            IActionResult response = services.GetRequiredService<IOptions<ApiBehaviorOptions>>().Value.InvalidModelStateResponseFactory(context);
            Assert.Equal(StatusCodes.Status400BadRequest, Assert.IsType<BadRequestObjectResult>(response).StatusCode);
        }
    }
}
