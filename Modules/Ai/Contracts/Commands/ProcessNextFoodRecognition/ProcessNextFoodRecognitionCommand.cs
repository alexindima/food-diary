using FoodDiary.Mediator;

namespace FoodDiary.Modules.Ai.Contracts.Commands.ProcessNextFoodRecognition;

// Claiming, provider calls and completion retain their independently committed lifecycle.
public sealed record ProcessNextFoodRecognitionCommand : IRequest<bool>;
