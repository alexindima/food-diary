using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.MailRelay.Presentation.Responses;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class ProducesMailRelayApiErrorResponseAttribute(int statusCode)
    : ProducesResponseTypeAttribute(typeof(MailRelayApiErrorHttpResponse), statusCode);
