using FoodDiary.Mediator;
using FoodDiary.Modules.Images.Application.Abstractions.Common;
using FoodDiary.Modules.Images.Domain.ValueObjects;
using FoodDiary.Modules.Images.Service.Contracts.Commands.ProcessOwnedImageDeletions;

namespace FoodDiary.Modules.Images.Application.Commands.ProcessOwnedImageDeletions;

public sealed class ProcessOwnedImageDeletionsCommandHandler(IImageObjectDeletionOutboxProcessor processor) : IRequestHandler<ProcessOwnedImageDeletionsCommand, int> {
    public Task<int> Handle(ProcessOwnedImageDeletionsCommand request, CancellationToken cancellationToken) {
        if (request.UserIds.Count is < 1 or > 20 || request.UserIds.Any(id => id == Guid.Empty) || request.ObjectKeys.Count > 1000) {
            throw new ArgumentException("A bounded owned image deletion scope is required.", nameof(request));
        }
        foreach (string key in request.ObjectKeys) {
            _ = ObjectStorageKey.FromInput(key);
            if (key.Contains('\\', StringComparison.Ordinal) || key.Split('/').Any(segment => segment is "." or "..") || !request.UserIds.Any(id => key.StartsWith($"users/{id:D}/images/", StringComparison.Ordinal))) {
                throw new InvalidOperationException("Image deletion refuses keys outside the fixture users.");
            }
        }
        return processor.ProcessKeysAsync(request.ObjectKeys, cancellationToken);
    }
}
