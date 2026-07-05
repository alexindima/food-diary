namespace FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;

public interface IUserRepository : IUserLookupRepository, IUserAdminReadRepository, IUserWriteRepository;
