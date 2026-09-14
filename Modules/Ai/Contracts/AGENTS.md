# AI consumer contracts

Own administration queries, prompt commands and completed recognition queries,
with their immutable DTOs. Use project-and-folder namespaces and preserve operation semantics.
Depend only on Mediator, Results and scalar Users.Domain.Contracts. Never expose aggregates,
job stores, provider clients or quota repositories here. The scheduler uses ProcessNextFoodRecognitionCommand; its implementation remains in Application.

Admin dispatches administration requests; Meals dispatches GetCompletedFoodRecognitionQuery. This query must retain ownership, completion and nutrition/vision consistency checks; GetFoodRecognitionQuery is not a substitute.
Moving these types does not change authorization, consent, quota, prompts, provider
requests, cancellation or serialized meal-creation/replay behavior. Implementations
remain in AI Application/Infrastructure. Deploy a coordinated rebuild.
