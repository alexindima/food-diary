using System.Reflection;
using FoodDiary.Modules.Admin.Presentation.Controllers;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace FoodDiary.Modules.Admin.Presentation.Tests;

[ExcludeFromCodeCoverage]
public sealed class AdminMailDependencyFailureTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MailDependencyTimeout_UsesTheControllerErrorCode(bool inbox) {
        Type controller = inbox ? typeof(AdminMailInboxController) : typeof(AdminOutgoingEmailsController);
        ExceptionFilterAttribute filter = Assert.Single(controller.GetCustomAttributes<ExceptionFilterAttribute>());
        var context = new ExceptionContext(new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()), []) {
            Exception = new OperationCanceledException("Synthetic timeout", new TimeoutException()),
        };

        filter.OnException(context);

        ObjectResult result = Assert.IsType<ObjectResult>(context.Result);
        ApiErrorHttpResponse error = Assert.IsType<ApiErrorHttpResponse>(result.Value);
        Assert.Multiple(
            () => Assert.True(context.ExceptionHandled),
            () => Assert.Equal(StatusCodes.Status502BadGateway, result.StatusCode),
            () => Assert.Equal(inbox ? "Admin.MailInboxUnavailable" : "Admin.OutgoingMailUnavailable", error.Error));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CancellationAndUnrelatedFailures_ArePreservedForTheOwningHandler(bool aborted) {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var http = new DefaultHttpContext { RequestAborted = aborted ? cancellation.Token : CancellationToken.None };
        ExceptionFilterAttribute filter = Assert.Single(typeof(AdminMailInboxController).GetCustomAttributes<ExceptionFilterAttribute>());
        Exception error = aborted ? new HttpRequestException("Synthetic disconnect") : new InvalidOperationException("Synthetic application failure");
        var context = new ExceptionContext(new ActionContext(http, new RouteData(), new ActionDescriptor()), []) { Exception = error };

        filter.OnException(context);

        Assert.Multiple(
            () => Assert.False(context.ExceptionHandled),
            () => Assert.Null(context.Result),
            () => Assert.Same(error, context.Exception));
    }
}
