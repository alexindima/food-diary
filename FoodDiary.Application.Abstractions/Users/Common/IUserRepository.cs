namespace FoodDiary.Application.Abstractions.Users.Common;

public interface IUserRepository : IUserLookupRepository, IUserAdminReadRepository, IUserWriteRepository;
