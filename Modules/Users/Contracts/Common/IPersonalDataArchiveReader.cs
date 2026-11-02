using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Users.Contracts.Common;

public interface IPersonalDataArchiveReader {
    Task<byte[]> ReadArchiveAsync(UserId userId, CancellationToken cancellationToken);
}
