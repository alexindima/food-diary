using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Runtime;
using FoodDiary.Mediator;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Application.Commands.UpsertAiPrompt;
using FoodDiary.Modules.Ai.Contracts.Commands.UpsertAiPrompt;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Modules.Ai.Application.Tests.Behaviors;

[ExcludeFromCodeCoverage]
public sealed class OwnerRequestTransactionTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NestedPromptMutation_LeavesCommitAndPostCommitActionsToOuterCommand(bool failAfterMutation) {
        IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.HasPendingChanges.Returns(returnThis: true);
        IPostCommitActionQueue actions = Substitute.For<IPostCommitActionQueue>();
        actions.HasActions.Returns(returnThis: true);
        IAiPromptTemplateWriteRepository repository = Substitute.For<IAiPromptTemplateWriteRepository>();
        var services = new ServiceCollection();
        services.AddApplicationRuntime();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton(unitOfWork);
        services.AddSingleton(actions);
        services.AddSingleton<IRequestHandler<UpsertAiPromptCommand, Result<AiPromptTemplateReadModel>>>(
            new UpsertAiPromptCommandHandler(repository));
        services.AddTransient<IRequestHandler<CompositeCommand, Result>>(provider =>
            new CompositeCommandHandler(provider.GetRequiredService<ISender>(), unitOfWork, actions));
        await using ServiceProvider provider = services.BuildServiceProvider();
        using var cancellation = new CancellationTokenSource();

        Result result = await provider.GetRequiredService<ISender>().Send(new CompositeCommand(failAfterMutation), cancellation.Token);

        Assert.Equal(failAfterMutation, result.IsFailure);
        if (failAfterMutation) {
            await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
            await actions.DidNotReceive().FlushAsync(Arg.Any<CancellationToken>());
        } else {
            await unitOfWork.Received(1).SaveChangesAsync(cancellation.Token);
            await actions.Received(1).FlushAsync(CancellationToken.None);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed record CompositeCommand(bool FailAfterMutation) : ICommand<Result>;

    [ExcludeFromCodeCoverage]
    private sealed class CompositeCommandHandler(ISender sender, IUnitOfWork unitOfWork, IPostCommitActionQueue actions)
        : IRequestHandler<CompositeCommand, Result> {
        public async Task<Result> Handle(CompositeCommand request, CancellationToken cancellationToken) {
            Result<AiPromptTemplateReadModel> changed = await sender.Send(
                new UpsertAiPromptCommand("test-prompt", "en", "prompt", IsActive: true), cancellationToken);
            Assert.True(changed.IsSuccess);
            await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
            await actions.DidNotReceive().FlushAsync(Arg.Any<CancellationToken>());
            return request.FailAfterMutation
                ? Result.Failure(new Error("Composite.Failed", "The outer operation failed.", ErrorKind.Conflict))
                : Result.Success();
        }
    }
}
