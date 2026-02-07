using System;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Authentication.Commands.VerifyEmail;

public record VerifyEmailCommand(
    UserId UserId,
    string Token) : ICommand<Result<bool>>;
