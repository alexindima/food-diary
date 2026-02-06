using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using FoodDiary.Application.Common.Abstractions.Result;

namespace FoodDiary.WebApi.Extensions;

public static class ResultExtensions
{
    /// <summary>
    /// Преобразует Result в IActionResult
    /// </summary>
    public static IActionResult ToActionResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
        {
            return new OkObjectResult(result.Value);
        }

        return result.Error.Code switch
        {
            var code when code.Contains("Authentication.InvalidToken") => new UnauthorizedObjectResult(new
            {
                error = result.Error.Code,
                message = result.Error.Message
            }),
            var code when code.Contains("Authentication.TelegramInvalidData") => new BadRequestObjectResult(new
            {
                error = result.Error.Code,
                message = result.Error.Message
            }),
            var code when code.Contains("Authentication.TelegramAuthExpired") => new UnauthorizedObjectResult(new
            {
                error = result.Error.Code,
                message = result.Error.Message
            }),
            var code when code.Contains("Authentication.TelegramNotLinked") => new NotFoundObjectResult(new
            {
                error = result.Error.Code,
                message = result.Error.Message
            }),
            var code when code.Contains("Authentication.TelegramAlreadyLinked") => new ConflictObjectResult(new
            {
                error = result.Error.Code,
                message = result.Error.Message
            }),
            var code when code.Contains("Authentication.AdminSsoForbidden") => new ObjectResult(new
            {
                error = result.Error.Code,
                message = result.Error.Message
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            },
            var code when code.Contains("Authentication.AdminSsoInvalidCode") => new UnauthorizedObjectResult(new
            {
                error = result.Error.Code,
                message = result.Error.Message
            }),
            var code when code.Contains("Ai.Forbidden") => new ObjectResult(new
            {
                error = result.Error.Code,
                message = result.Error.Message
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            },
            var code when code.Contains("Ai.QuotaExceeded") => new ObjectResult(new
            {
                error = result.Error.Code,
                message = result.Error.Message
            })
            {
                StatusCode = StatusCodes.Status429TooManyRequests
            },
            var code when code.Contains("Ai.OpenAiFailed") || code.Contains("Ai.InvalidResponse") => new ObjectResult(new
            {
                error = result.Error.Code,
                message = result.Error.Message
            })
            {
                StatusCode = StatusCodes.Status502BadGateway
            },
            var code when code.Contains("NotFound") => new NotFoundObjectResult(new
            {
                error = result.Error.Code,
                message = result.Error.Message
            }),
            var code when code.Contains("Validation") => new BadRequestObjectResult(new
            {
                error = result.Error.Code,
                message = result.Error.Message
            }),
            var code when code.Contains("AlreadyExists") => new ConflictObjectResult(new
            {
                error = result.Error.Code,
                message = result.Error.Message
            }),
            _ => new ObjectResult(new
            {
                error = result.Error.Code,
                message = result.Error.Message
            })
            {
                StatusCode = 500
            }
        };
    }
}
