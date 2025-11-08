namespace FoodDiary.Application.Common.Abstractions.Result;

/// <summary>
/// Содержит стандартные типы ошибок
/// </summary>
public static class Errors
{
    public static class Product
    {
        public static Error NotFound(int id) => new(
            "Product.NotFound",
            $"Продукт с ID {id} не найден");

        public static Error AlreadyExists(string barcode) => new(
            "Product.AlreadyExists",
            $"Продукт с баркодом {barcode} уже существует");

        public static Error InvalidData(string message) => new(
            "Product.InvalidData",
            message);
    }

    public static class User
    {
        public static Error NotFound(int id) => new(
            "User.NotFound",
            $"Пользователь с ID {id} не найден");

        public static Error InvalidCredentials => new(
            "User.InvalidCredentials",
            "Неверный email или пароль");

        public static Error EmailAlreadyExists => new(
            "User.EmailAlreadyExists",
            "Пользователь с таким email уже существует");
    }

    public static class Validation
    {
        public static Error Required(string field) => new(
            "Validation.Required",
            $"Поле {field} обязательно для заполнения");

        public static Error Invalid(string field, string reason) => new(
            "Validation.Invalid",
            $"Поле {field} невалидно: {reason}");
    }
}
