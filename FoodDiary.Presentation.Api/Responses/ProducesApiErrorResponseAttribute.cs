using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Responses;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class ProducesApiErrorResponseAttribute(int statusCode)
    : ProducesResponseTypeAttribute(typeof(ApiErrorHttpResponse), statusCode);
