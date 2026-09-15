namespace FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;

/// <summary>A top-level command whose handler and coordinated save share one retriable transaction.</summary>
/// <remarks>Handlers must not perform external side effects or start nested transactions.</remarks>
public interface IAtomicCommand : ITransactionalCommand;
