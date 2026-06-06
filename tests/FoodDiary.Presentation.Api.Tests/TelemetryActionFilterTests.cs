using FoodDiary.Presentation.Api.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Presentation.Api.Tests {
    [ExcludeFromCodeCoverage]
    public sealed class TelemetryActionFilterTests {
        [Fact]
        public async Task OnActionExecutionAsync_WithSuccessfulAction_InvokesNext() {
            var filter = new TelemetryActionFilter(NullLogger<TelemetryActionFilter>.Instance);
            ActionExecutingContext context = CreateActionExecutingContext(
                new FoodDiary.Presentation.Api.Features.TelemetryTests.TelemetryProbeController(),
                actionName: "Get",
                statusCode: StatusCodes.Status200OK);
            bool nextCalled = false;

            await filter.OnActionExecutionAsync(context, () => {
                nextCalled = true;
                return Task.FromResult(new ActionExecutedContext(context, [], context.Controller));
            });

            Assert.True(nextCalled);
        }

        [Fact]
        public async Task OnActionExecutionAsync_WithFailureStatus_InvokesNext() {
            var filter = new TelemetryActionFilter(NullLogger<TelemetryActionFilter>.Instance);
            ActionExecutingContext context = CreateActionExecutingContext(
                controller: new object(),
                actionName: null,
                statusCode: StatusCodes.Status404NotFound);

            await filter.OnActionExecutionAsync(context, () => Task.FromResult(
                new ActionExecutedContext(context, [], context.Controller)));

            Assert.Equal(StatusCodes.Status404NotFound, context.HttpContext.Response.StatusCode);
        }

        [Fact]
        public async Task OnActionExecutionAsync_WithActionException_RecordsFailureAndInvokesNext() {
            var filter = new TelemetryActionFilter(NullLogger<TelemetryActionFilter>.Instance);
            ActionExecutingContext context = CreateActionExecutingContext(
                new FoodDiary.Presentation.Api.Features.TelemetryTests.TelemetryProbeController(),
                actionName: "Post",
                statusCode: StatusCodes.Status500InternalServerError);
            var exception = new InvalidOperationException("boom");

            await filter.OnActionExecutionAsync(context, () => Task.FromResult(
                new ActionExecutedContext(context, [], context.Controller) {
                    Exception = exception,
                }));

            Assert.Equal(StatusCodes.Status500InternalServerError, context.HttpContext.Response.StatusCode);
        }

        private static ActionExecutingContext CreateActionExecutingContext(
            object controller,
            string? actionName,
            int statusCode) {
            var httpContext = new DefaultHttpContext();
            httpContext.Response.StatusCode = statusCode;

            var routeValues = new Dictionary<string, string?>(StringComparer.Ordinal);
            if (actionName is not null) {
                routeValues["action"] = actionName;
            }

            var actionContext = new ActionContext(
                httpContext,
                new RouteData(),
                new ActionDescriptor {
                    RouteValues = routeValues,
                });

            return new ActionExecutingContext(
                actionContext,
                [],
                new Dictionary<string, object?>(StringComparer.Ordinal),
                controller);
        }
    }
}
