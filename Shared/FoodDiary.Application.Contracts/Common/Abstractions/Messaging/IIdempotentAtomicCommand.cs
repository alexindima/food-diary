using FoodDiary.Application.Contracts.Common.Abstractions.Persistence;

namespace FoodDiary.Application.Contracts.Common.Abstractions.Messaging;

public interface IIdempotentAtomicCommand : IAtomicCommand {
    AtomicOperation? Operation { get; }
}
