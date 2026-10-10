import type { UsdaService } from "@candidate/app/features/usda/api/usda.service";
import type { UsdaProductLink } from "@candidate/app/features/usda/contracts/usda-product-link";
import { fastingCycleDays } from "@candidate/app/features/fasting/models/fasting-start-intent";
import { entityId } from "@candidate/app/shared/models/semantics/entity-id";
import { usdaFoodId } from "@candidate/app/shared/models/semantics/usda-food-id";

declare const usda: UsdaService;
declare const link: UsdaProductLink;
const product = entityId<"product">("owned");
const recipe = entityId<"recipe">("foreign");
const food = usdaFoodId(17000);
usda.linkProduct(product, food);
link.linkProduct(product, food);
usda.getFoodDetail(food);
// @ts-expect-error A recipe is not the owning product.
link.unlinkProduct(recipe);
// @ts-expect-error A cycle duration is not an external USDA identity.
usda.getFoodDetail(fastingCycleDays(2));
// @ts-expect-error Quantities cannot be silently retagged as external keys.
usdaFoodId(fastingCycleDays(2));
