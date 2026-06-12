using FoodDiary.MailRelay.Application.Common.Results;
using FoodDiary.Mediator;

namespace FoodDiary.MailRelay.Presentation.Controllers;

public static class MailRelayMappedRequest {
    public static MailRelayMappedRequest<TResponse> Success<TResponse>(IRequest<Result<TResponse>> request) =>
        new(request, Error: null);

    public static MailRelayMappedRequest<TResponse> Failure<TResponse>(string? error) =>
        new(Request: null, error);
}

public sealed record MailRelayMappedRequest<TResponse>(IRequest<Result<TResponse>>? Request, string? Error) {
    public bool IsSuccess => Request is not null;
}
