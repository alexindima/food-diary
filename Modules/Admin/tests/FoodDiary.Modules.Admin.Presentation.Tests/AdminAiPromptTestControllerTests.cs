using System.Reflection;
using FoodDiary.Presentation.Api.Tests;
using FoodDiary.Presentation.Api.Filters;
using FoodDiary.Modules.Admin.Application.Commands.TestAdminAiPrompt;
using FoodDiary.Modules.Admin.Presentation.Controllers;
using FoodDiary.Modules.Admin.Presentation.Requests;
using FoodDiary.Modules.Admin.Presentation.Responses;
using FoodDiary.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.Admin.Presentation.Tests;

[ExcludeFromCodeCoverage]
public sealed class AdminAiPromptTestControllerTests {
    [Fact]
    public async Task Test_MapsDraftCallerAndIdempotencyContextToCommand() {
        CapturedSender sender = SubstituteSender.Capture(Result.Success("sample-json"));
        var context = new DefaultHttpContext();
        MethodInfo setter = typeof(IdempotencyRequestContext).GetMethod("SetRequestId", BindingFlags.Static | BindingFlags.NonPublic)!;
        setter.Invoke(null, [context, "request-id"]);
        var controller = new AdminAiPromptsController(sender) {
            ControllerContext = new ControllerContext { HttpContext = context },
        };
        var user = Guid.NewGuid();
        var request = new AdminAiPromptDraftHttpRequest("nutrition", "ru", "Estimate", "hint", Guid.NewGuid(), "apple", 100, "g");
        AdminAiPromptInspectionHttpResponse response = Assert.IsType<AdminAiPromptInspectionHttpResponse>(Assert.IsType<OkObjectResult>(await controller.Test(user, request)).Value);
        TestAdminAiPromptCommand command = Assert.IsType<TestAdminAiPromptCommand>(sender.Request);
        Assert.Multiple(() => Assert.Equal(user, command.UserId), () => Assert.Equal("request-id", command.RequestId),
            () => Assert.Equivalent(request, command.Draft, strict: true),
            () => Assert.Equivalent(new AdminAiPromptInspectionHttpResponse("sample-json"), response, strict: true));
    }
}
