import type { CycleEpisodeFacade } from '../src/app/features/cycle-tracking/lib/cycle-episode.facade';
import type { CycleTrackingFacade } from '../src/app/features/cycle-tracking/lib/cycle-tracking.facade';
import type { CycleTrackingStateFacade } from '../src/app/features/cycle-tracking/lib/cycle-tracking-state.facade';
import { fastingCycleDays } from '../src/app/features/fasting/models/fasting-start-intent';
import type { ProductExternalFoodFacade } from '../src/app/features/products/lib/manage/product-external-food.facade';
import type { UsdaService } from '../src/app/features/usda/api/usda.service';
import type { UsdaProductLink } from '../src/app/features/usda/contracts/usda-product-link';
import { entityId } from '../src/app/shared/models/semantics/entity-id';
import { usdaFoodId } from '../src/app/shared/models/semantics/usda-food-id';

const FOOD_KEY = 17_000;

function semanticPorts({
    usda,
    link,
    external,
    episode,
    cycle,
    state,
}: {
    usda: UsdaService;
    link: UsdaProductLink;
    external: ProductExternalFoodFacade;
    episode: CycleEpisodeFacade;
    cycle: CycleTrackingFacade;
    state: CycleTrackingStateFacade;
}): void {
    const productId = entityId<'product'>('local-product');
    const recipeId = entityId<'recipe'>('local-recipe');
    const episodeId = entityId<'menstrual-episode'>('local-episode');
    const foodId = usdaFoodId(FOOD_KEY);
    const days = fastingCycleDays(2);
    usda.linkProduct(productId, foodId);
    link.linkProduct(productId, foodId);
    external.linkUsdaProduct(productId, foodId);
    usda.getFoodDetail(foodId);
    episode.editMenstrualEpisode(episodeId);
    void episode.deleteMenstrualEpisodeAsync(episodeId);
    void cycle.toggleMenstrualEpisodePredictionAsync(episodeId);
    state.editingEpisodeId.set(episodeId);
    // @ts-expect-error A recipe cannot identify a product link.
    usda.linkProduct(recipeId, foodId);
    // @ts-expect-error The published link capability preserves owning product identity.
    link.unlinkProduct(recipeId);
    // @ts-expect-error The cross-feature facade preserves owning product identity.
    external.linkUsdaProduct(recipeId, foodId);
    // @ts-expect-error A cycle-day count is not a USDA food key.
    usda.getFoodDetail(days);
    // @ts-expect-error Numeric meaning constructors cannot silently retag a quantity as an external key.
    usdaFoodId(days);
    // @ts-expect-error External keys cannot silently become quantities.
    fastingCycleDays(foodId);
    // @ts-expect-error Episode actions retain decoded ownership.
    episode.editMenstrualEpisode(productId);
    // @ts-expect-error Episode deletion cannot accept a recipe.
    void episode.deleteMenstrualEpisodeAsync(recipeId);
    // @ts-expect-error Orchestration retains episode ownership.
    void cycle.deleteMenstrualEpisodeAsync(productId);
    // @ts-expect-error Editing state retains episode ownership.
    state.editingEpisodeId.set(recipeId);
    // @ts-expect-error Busy deletion state retains episode ownership.
    state.deletingEpisodeId.set(productId);
    // @ts-expect-error Prediction toggle state retains episode ownership.
    state.excludingEpisodeId.set(productId);
}
void semanticPorts;
