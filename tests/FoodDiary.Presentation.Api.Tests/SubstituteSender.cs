using FoodDiary.Mediator;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
internal static class SubstituteSender {
    public static ISender Create<TResponse>(TResponse response, Action<IRequest<TResponse>>? capture = null) {
        ISender sender = Substitute.For<ISender>();
        sender.Send(Arg.Do<IRequest<TResponse>>(request => capture?.Invoke(request)), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));
        return sender;
    }
}
