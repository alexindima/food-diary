using System.Security.Cryptography;
using System.Text;
using FoodDiary.BugTriage.Presentation.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace FoodDiary.BugTriage.Presentation.Security;

public sealed class BugTriageReadAuthorizationFilter(IOptions<BugTriageHttpOptions> options) : IAuthorizationFilter {
    public void OnAuthorization(AuthorizationFilterContext context) {
        string expected = options.Value.ReadApiKey;
        if (expected.Length < 32 ||
            !context.HttpContext.Request.Headers.TryGetValue("X-BugTriage-Read-Key", out Microsoft.Extensions.Primitives.StringValues values) ||
            values.Count != 1 || values[0] is not { Length: >= 32 and <= 256 } supplied ||
            !CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(expected)), SHA256.HashData(Encoding.UTF8.GetBytes(supplied)))) {
            context.Result = new UnauthorizedResult();
        }
    }
}
