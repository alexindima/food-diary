using FoodDiary.Mediator;

namespace FoodDiary.Modules.Images.Service.Contracts.Commands.ProcessOwnedImageDeletions;

public sealed record ProcessOwnedImageDeletionsCommand(IReadOnlyList<Guid> UserIds, IReadOnlyList<string> ObjectKeys) : IRequest<int>;
