# AI consumer contracts

Own administration reads, prompt administration and completed recognition reads,
with their immutable DTOs. Preserve existing CLR namespaces and method signatures.
Depend only on Results and scalar Users.Domain.Contracts. Never expose aggregates,
job stores, provider clients, quota repositories or processing entrypoints here.

Admin consumes administration capabilities; Meals consumes IFoodRecognitionResultReader.
Moving these types does not change authorization, consent, quota, prompts, provider
requests, cancellation or serialized meal-creation/replay behavior. Implementations
remain in AI Application/Infrastructure. Deploy a coordinated rebuild.
