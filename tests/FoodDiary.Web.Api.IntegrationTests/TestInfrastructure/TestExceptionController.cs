using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

[ApiController]
[Route("test/exceptions")]
public sealed class TestExceptionController : ControllerBase {
    [HttpGet("unhandled")]
    public IActionResult ThrowUnhandled() {
        throw new InvalidOperationException("Unhandled test exception.");
    }
}
