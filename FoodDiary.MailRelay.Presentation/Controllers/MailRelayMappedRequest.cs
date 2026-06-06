using FoodDiary.MailRelay.Application.Common.Results;
using FoodDiary.Mediator;

namespace FoodDiary.MailRelay.Presentation.Controllers;

public sealed record MailRelayMappedRequest<TResponse>(IRequest<Result<TResponse>>? Request, string? Error) {
    public bool IsSuccess => Request is not null;

    public static MailRelayMappedRequest<TResponse> Success(IRequest<Result<TResponse>> request) =>
        new(request, Error: null);

    public static MailRelayMappedRequest<TResponse> Failure(string? error) =>
        new(Request: null, error);
}
