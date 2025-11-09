using System;

namespace FoodDiary.Application.Common.Abstractions.Result;

public static class Errors
{
    public static class Product
    {
        public static Error NotFound(Guid id) => new(
            "Product.NotFound",
            $"Product with ID {id} was not found.");

        public static Error NotAccessible(Guid id) => new(
            "Product.NotAccessible",
            $"Product with ID {id} does not belong to the current user or was not found.");

        public static Error AlreadyExists(string barcode) => new(
            "Product.AlreadyExists",
            $"Product with barcode {barcode} already exists.");

        public static Error InvalidData(string message) => new(
            "Product.InvalidData",
            message);
    }

    public static class User
    {
        public static Error NotFound(Guid id) => new(
            "User.NotFound",
            $"User with ID {id} was not found.");

        public static Error InvalidPassword => new(
            "User.InvalidPassword",
            "The current password is incorrect.");

        public static Error NotFound() => new(
            "User.NotFound",
            "User was not found.");

        public static Error InvalidCredentials => new(
            "User.InvalidCredentials",
            "Invalid email or password.");

        public static Error EmailAlreadyExists => new(
            "User.EmailAlreadyExists",
            "A user with this email already exists.");
    }

    public static class Authentication
    {
        public static Error InvalidCredentials => new(
            "Authentication.InvalidCredentials",
            "Invalid email or password.");

        public static Error InvalidToken => new(
            "Authentication.InvalidToken",
            "Invalid authorization token.");
    }

    public static class Validation
    {
        public static Error Required(string field) => new(
            "Validation.Required",
            $"Field {field} is required.");

        public static Error Invalid(string field, string reason) => new(
            "Validation.Invalid",
            $"Field {field} is invalid: {reason}");
    }
}
