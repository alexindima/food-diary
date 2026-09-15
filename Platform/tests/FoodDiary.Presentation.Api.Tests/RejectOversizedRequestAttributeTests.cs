using FoodDiary.Presentation.Api.Filters;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class RejectOversizedRequestAttributeTests {
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithNonPositiveLimit_Throws(long maxBytes) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RejectOversizedRequestAttribute(maxBytes));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(99L)]
    [InlineData(100L)]
    public void OnResourceExecuting_WhenContentLengthIsWithinLimit_AllowsRequest(long? contentLength) {
        var attribute = new RejectOversizedRequestAttribute(100);
        ResourceExecutingContext context = CreateExecutingContext(contentLength);

        attribute.OnResourceExecuting(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public void OnResourceExecuting_WhenContentLengthExceedsLimit_ReturnsStandardApiError() {
        var attribute = new RejectOversizedRequestAttribute(100);
        ResourceExecutingContext context = CreateExecutingContext(101);
        context.HttpContext.TraceIdentifier = "trace-413";

        attribute.OnResourceExecuting(context);

        ObjectResult result = Assert.IsType<ObjectResult>(context.Result);
        ApiErrorHttpResponse response = Assert.IsType<ApiErrorHttpResponse>(result.Value);
        Assert.Multiple(
            () => Assert.Equal(StatusCodes.Status413PayloadTooLarge, result.StatusCode),
            () => Assert.Equal("Request.PayloadTooLarge", response.Error),
            () => Assert.Equal("The request payload is too large.", response.Message),
            () => Assert.Equal("trace-413", response.TraceId),
            () => Assert.Equal(100, attribute.MaxBytes),
            () => Assert.Equal(int.MinValue + 100, attribute.Order));
    }

    [Fact]
    public void OnResourceExecuted_DoesNotMutateContext() {
        var attribute = new RejectOversizedRequestAttribute(100);
        ResourceExecutingContext executingContext = CreateExecutingContext(10);
        var executedContext = new ResourceExecutedContext(executingContext, []);

        attribute.OnResourceExecuted(executedContext);

        Assert.Null(executedContext.Result);
    }

    private static ResourceExecutingContext CreateExecutingContext(long? contentLength) {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.ContentLength = contentLength;
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor(),
            new ModelStateDictionary());
        return new ResourceExecutingContext(actionContext, [], []);
    }
}
