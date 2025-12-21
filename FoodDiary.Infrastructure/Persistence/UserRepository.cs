using Microsoft.EntityFrameworkCore;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Infrastructure.Persistence;

public class UserRepository : IUserRepository
{
    private readonly FoodDiaryDbContext _context;

    public UserRepository(FoodDiaryDbContext context) => _context = context;

    public async Task<User?> GetByEmailAsync(string email) =>
        await _context.Users.FirstOrDefaultAsync(u =>
            u.Email == email && u.IsActive && u.DeletedAt == null);

    public async Task<User?> GetByEmailIncludingDeletedAsync(string email) =>
        await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

    public async Task<User?> GetByIdAsync(UserId id) =>
        await _context.Users.FirstOrDefaultAsync(u =>
            u.Id == id && u.IsActive && u.DeletedAt == null);

    public async Task<User> AddAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }
}
