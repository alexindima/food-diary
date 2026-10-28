using FoodDiary.Mediator;
using FoodDiary.Authentication.Contracts.Authentication.Common;
using FoodDiary.Modules.Identity.Application.Authentication.Commands.RestoreBrowserSession;
using FoodDiary.Modules.Identity.Application.Authentication.Models;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Controllers;
using FoodDiary.Modules.Identity.Presentation.Security;
using FoodDiary.Presentation.Api.Tests;
using FoodDiary.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute.Core;

namespace FoodDiary.Modules.Identity.Presentation.Tests;

[ExcludeFromCodeCoverage]
public sealed class BrowserSessionRestorationDiagnosticsTests {
    public static TheoryData<bool, bool, bool, bool> CookieCases {
        get {
            var cases = new TheoryData<bool, bool, bool, bool>();
            foreach (bool https in new[] { false, true }) {
                foreach (bool admin in new[] { false, true }) {
                    foreach (bool selected in new[] { false, true }) {
                        foreach (bool alternate in new[] { false, true }) {
                            cases.Add(https, admin, selected, alternate);
                        }
                    }
                }
            }
            return cases;
        }
    }

    [Theory]
    [MemberData(nameof(CookieCases))]
    public async Task Restore_RejectedRequestLogsOnlyTransportBooleans(bool https, bool admin, bool selected, bool alternate) {
        ILogger<BrowserSessionController> logger = Substitute.For<ILogger<BrowserSessionController>>();
        await using ServiceProvider services = new ServiceCollection()
            .AddSingleton(logger).AddSingleton<BrowserSessionCookieService>().BuildServiceProvider();
        RestoreBrowserSessionCommand? sent = null;
        ISender sender = SubstituteSender.Create(Result.Failure<BrowserSessionModel>(AuthenticationErrors.InvalidToken),
            request => sent = Assert.IsType<RestoreBrowserSessionCommand>(request));
        var controller = new BrowserSessionController(sender) {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { RequestServices = services } },
        };
        controller.Request.Scheme = https ? "https" : "http";
        controller.Request.Headers[BrowserSessionCookieService.ClientHeader] = admin ? "admin" : "web-private-canary";
        controller.Request.Headers.Authorization = "Bearer private-authorization-canary";
        controller.Request.Headers.UserAgent = "private-user-agent-canary";
        string family = admin ? BrowserSessionCookieService.AdminCookieName : BrowserSessionCookieService.BrowserCookieName;
        var cookies = new List<string>();
        if (selected) {
            cookies.Add($"{(https ? "__Host-" : string.Empty)}{family}=private-selected-cookie");
        }
        if (alternate) {
            cookies.Add($"{(https ? string.Empty : "__Host-")}{family}=private-alternate-cookie");
        }
        controller.Request.Headers.Cookie = string.Join("; ", cookies);

        ObjectResult result = Assert.IsAssignableFrom<ObjectResult>(await controller.Restore());

        Assert.Equal(StatusCodes.Status401Unauthorized, result.StatusCode);
        Assert.Equal(selected ? "private-selected-cookie" : null, sent!.Credential);
        Assert.Equal(0, controller.Response.Headers.SetCookie.Count);
        ICall log = Assert.Single(logger.ReceivedCalls(), call => string.Equals(call.GetMethodInfo().Name, nameof(ILogger.Log), StringComparison.Ordinal));
        var fields = Assert.IsAssignableFrom<IEnumerable<KeyValuePair<string, object?>>>(log.GetArguments()[2]).ToDictionary(StringComparer.Ordinal);
        Assert.Equal(new[] { "Admin", "AlternateCookiePresent", "CookiePresent", "Https", "{OriginalFormat}" }, fields.Keys.Order(StringComparer.Ordinal).ToArray());
        Assert.Equal(selected, fields["CookiePresent"]);
        Assert.Equal(alternate, fields["AlternateCookiePresent"]);
        Assert.Equal(https, fields["Https"]);
        Assert.Equal(admin, fields["Admin"]);
        Assert.Equal("BrowserSessionRestoreTransportRejected", Assert.IsType<EventId>(log.GetArguments()[1]).Name);
        Assert.Null(log.GetArguments()[3]);
    }
}
