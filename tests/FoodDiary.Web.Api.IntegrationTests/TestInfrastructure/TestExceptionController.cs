using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

[ApiController]
[ApiVersionNeutral]
[Route("test/exceptions")]
[ExcludeFromCodeCoverage]
public sealed class TestExceptionController : ControllerBase {
    [HttpGet("unhandled")]
    public IActionResult ThrowUnhandled() {
        throw new InvalidOperationException("Unhandled test exception.");
    }

    [HttpGet("concurrency")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult ThrowConcurrency() {
        throw new DbUpdateConcurrencyException("Concurrency test exception.");
    }
}
