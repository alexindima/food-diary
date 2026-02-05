namespace FoodDiary.Contracts.Authentication;

public sealed record AdminSsoExchangeRequest(
    string Code
);
