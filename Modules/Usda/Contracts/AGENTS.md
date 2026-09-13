# USDA Contracts

Own stable USDA read DTOs and the cross-module capabilities for food search,
local suggestions, product linking and meal nutrition input. Preserve CLR namespaces,
method signatures, result errors and cancellation behavior. Depend only on Results
and scalar Products/Users Domain.Contracts; never reference aggregates or storage.

USDA supplies food search and suggestion services. Products implements
IUsdaProductLinkService and retains ownership/access checks and aggregate writes.
Meals implements IUsdaMealNutritionReadService through its read repository.
These consumer-owned integration ports belong here so suppliers need not acquire
USDA repositories. Provider HTTP/options/cache implementations remain Infrastructure;
USDA repositories and the internal daily-summary service stay in Abstractions.
