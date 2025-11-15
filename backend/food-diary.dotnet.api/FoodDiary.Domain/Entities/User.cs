using FoodDiary.Domain.Common;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Entities;

/// <summary>
/// Пользователь системы - корень агрегата
/// </summary>
public sealed class User : AggregateRoot<UserId> {
    public string Email { get; private set; } = string.Empty;
    public string Password { get; private set; } = string.Empty;
    public string? RefreshToken { get; private set; }
    public string? Username { get; private set; }
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public DateTime? BirthDate { get; private set; }
    public string? Gender { get; private set; }
    public double? Weight { get; private set; }
    public double? DesiredWeight { get; private set; }
    public double? Height { get; private set; }
    public string? ProfileImage { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Navigation properties
    public ICollection<Meal> Meals { get; private set; } = new List<Meal>();
    public ICollection<Product> Products { get; private set; } = new List<Product>();
    public ICollection<Recipe> Recipes { get; private set; } = new List<Recipe>();
    public ICollection<WeightEntry> WeightEntries { get; private set; } = new List<WeightEntry>();

    // Конструктор для EF Core
    private User() {
    }

    // Factory method для создания нового пользователя
    public static User Create(string email, string hashedPassword) {
        var user = new User {
            Id = UserId.New(),
            Email = email,
            Password = hashedPassword
        };
        user.SetCreated();
        return user;
    }

    public void UpdateRefreshToken(string? refreshToken) {
        RefreshToken = refreshToken;
        SetModified();
    }

    public void UpdatePassword(string hashedPassword) {
        Password = hashedPassword;
        SetModified();
    }

    public void UpdateProfile(
        string? username = null,
        string? firstName = null,
        string? lastName = null,
        DateTime? birthDate = null,
        string? gender = null,
        double? weight = null,
        double? height = null,
        string? profileImage = null) {
        if (username is not null) Username = username;
        if (firstName is not null) FirstName = firstName;
        if (lastName is not null) LastName = lastName;
        if (birthDate.HasValue) BirthDate = birthDate;
        if (gender is not null) Gender = gender;
        if (weight.HasValue) Weight = weight;
        if (height.HasValue) Height = height;
        if (profileImage is not null) ProfileImage = profileImage;

        SetModified();
    }

    public void UpdateDesiredWeight(double? desiredWeight)
    {
        DesiredWeight = desiredWeight;
        SetModified();
    }

    public void Deactivate() {
        IsActive = false;
        SetModified();
    }

    public void Activate() {
        IsActive = true;
        SetModified();
    }
}
