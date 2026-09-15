using System.Diagnostics;
using FoodDiary.Application.Runtime.Common.Services;
using FoodDiary.Mediator;
using FoodDiary.Results;

namespace FoodDiary.Application.Runtime.Common.Behaviors;

public sealed class ModuleTelemetryBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse> {
    private static readonly string Module = ModuleOperationTelemetry.ResolveModule(typeof(TRequest).Assembly.GetName().Name);
    private static readonly string Operation = typeof(TRequest).Name;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken) {
        long started = Stopwatch.GetTimestamp();
        string outcome = "exception";
        ModuleOperationTelemetry.RecordActive(Module, Operation, 1);

        try {
            TResponse response = await next(cancellationToken).ConfigureAwait(false);
            outcome = response is Result { IsFailure: true } ? "failure" : "success";
            return response;
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            outcome = "cancelled";
            throw;
        } finally {
            ModuleOperationTelemetry.RecordActive(Module, Operation, -1);
            ModuleOperationTelemetry.RecordCompleted(Module, Operation, outcome, Stopwatch.GetElapsedTime(started));
        }
    }
}
