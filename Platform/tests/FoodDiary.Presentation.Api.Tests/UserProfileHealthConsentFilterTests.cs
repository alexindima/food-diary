using System.Security.Claims;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Presentation.Filters;
using FoodDiary.Modules.Users.Presentation.Requests;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class UserProfileHealthConsentFilterTests {
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ProfileCorrection_RemainsAvailableWhileHealthWritesRequireConsent(bool healthWrite, bool granted) {
        var id = Guid.NewGuid();
        var http = new DefaultHttpContext {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, id.ToString())], "test")),
        };
        var action = new ActionContext(http, new RouteData(), new ActionDescriptor());
        object controller = new();
        var context = new ActionExecutingContext(action, [], new Dictionary<string, object?>(StringComparer.Ordinal) {
            ["request"] = new UpdateUserHttpRequest { FirstName = "Corrected", WeightKg = healthWrite ? 65 : null },
        }, controller);
        IUserPrivacyStateReader reader = Substitute.For<IUserPrivacyStateReader>();
        reader.GetAsync(new UserId(id), Arg.Any<CancellationToken>()).Returns(new UserPrivacyStateModel("2026-11-02",
            granted ? DateTime.UtcNow : null, AiAcceptedAtUtc: null, MarketingAcceptedAtUtc: null));
        bool continued = false;

        await new UserProfileHealthConsentFilter(reader).OnActionExecutionAsync(context, () => {
            continued = true;
            return Task.FromResult(new ActionExecutedContext(action, [], controller));
        });

        Assert.Equal(!healthWrite || granted, continued);
        if (healthWrite && !granted) {
            Assert.Equal(403, Assert.IsType<ObjectResult>(context.Result).StatusCode);
        }
        if (!healthWrite) {
            await reader.DidNotReceiveWithAnyArgs().GetAsync(default, default);
        }
    }
}
