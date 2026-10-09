import {
    type CatalogRecipeServings,
    decodeCatalogIngredientSource,
} from '../projects/fooddiary-admin/src/app/features/admin-catalog/models/catalog-ingredient-source';
import {
    adminCalendarDate,
    type AdminId,
    adminId,
    adminUtcInstant,
} from '../projects/fooddiary-admin/src/app/shared/models/semantics/admin-meaning';
import type { MealActions } from '../src/app/features/meals/contracts/meal-actions';
import type { ImageSelection } from '../src/app/shared/models/image-upload.data';
import { MealSourceType, type ProductMealItem, type RecipeMealItem } from '../src/app/shared/models/meal.data';
import {
    type CalendarDate,
    calendarDate,
    optionalCalendarDate,
    type UtcInstant,
    utcInstant,
} from '../src/app/shared/models/semantics/date-value';
import { entityId, type MealId, optionalEntityId, type ProductId } from '../src/app/shared/models/semantics/entity-id';
import { publicImageUrl, signedImageUploadUrl } from '../src/app/shared/models/semantics/image-location';
import {
    type ProductQuantity,
    productQuantity,
    type RecipeServings,
    recipeServings,
} from '../src/app/shared/models/semantics/meal-quantity';
import type { User, WeightGoalHistoryItem } from '../src/app/shared/models/user.data';

const PRODUCT_UNITS = 100;

export function verifyMeaning(actions: MealActions): void {
    const meal: MealId = entityId<'meal'>('placeholder');
    const product: ProductId = entityId<'product'>('placeholder');
    const day: CalendarDate = calendarDate('2026-10-08');
    const instant: UtcInstant = utcInstant('2026-10-08T12:00:00.1234567Z');
    const grams: ProductQuantity = productQuantity(PRODUCT_UNITS);
    const servings: RecipeServings = recipeServings(2);
    const absentId: MealId | undefined = optionalEntityId<'meal'>(undefined);
    const absentDay: null = optionalCalendarDate(null);
    void absentId;
    void absentDay;
    actions.repeat(meal, instant, 'Lunch');
    // @ts-expect-error A product identity must not reach a meal mutation.
    actions.deleteById(product);
    // @ts-expect-error Calendar days must not be passed as meal timestamps.
    actions.repeat(meal, day, 'Lunch');
    // @ts-expect-error Rebranding an already-typed foreign ID is forbidden.
    entityId<'meal'>(product);
    // @ts-expect-error Rebranding an instant as a day is forbidden.
    calendarDate(instant);
    // @ts-expect-error Entity identities cannot be passed to a timestamp factory.
    utcInstant(product);
    // @ts-expect-error Calendar days cannot be passed to an identity factory.
    entityId<'meal'>(day);
    // @ts-expect-error Recipe servings are not product units.
    productQuantity(servings);
    // @ts-expect-error Product units are not recipe servings.
    recipeServings(grams);
    const identity = { id: entityId<'meal-item'>('item'), mealId: meal };
    const wrongProduct: ProductMealItem = {
        ...identity,
        sourceType: MealSourceType.Product,
        // @ts-expect-error The product branch requires ProductQuantity.
        amount: servings,
        product: null,
        recipe: null,
    };
    // @ts-expect-error The recipe branch requires RecipeServings.
    const wrongRecipe: RecipeMealItem = { ...identity, sourceType: MealSourceType.Recipe, amount: grams, product: null, recipe: null };
    void wrongProduct;
    void wrongRecipe;
    const profile: Pick<User, 'id' | 'birthDate'> = { id: entityId<'user'>('user'), birthDate: day };
    // @ts-expect-error Birth dates use a calendar meaning.
    profile.birthDate = instant;
    // @ts-expect-error A meal identity cannot address a user profile.
    profile.id = meal;
    const goal: Pick<WeightGoalHistoryItem, 'id' | 'startedAtUtc'> = { id: entityId<'weight-goal'>('goal'), startedAtUtc: instant };
    // @ts-expect-error Weight goal history is independent from a weight measurement.
    goal.id = entityId<'weight-entry'>('entry');
    // @ts-expect-error Goal history timestamps are instants.
    goal.startedAtUtc = day;
    void profile;
    void goal;
    const location = publicImageUrl('relative/photo');
    const upload = signedImageUploadUrl('https://storage.example/photo?sig=encoded');
    // @ts-expect-error A signed upload target cannot become a public image location.
    publicImageUrl(upload);
    // @ts-expect-error Public image locations cannot address an upload request.
    signedImageUploadUrl(location);
    // @ts-expect-error Uploaded selections require an image asset identity.
    const wrongSelection: ImageSelection = { kind: 'uploaded', url: location, assetId: null };
    // @ts-expect-error An image asset cannot use a product identity.
    const wrongAsset: ImageSelection = { kind: 'uploaded', url: location, assetId: product };
    const adminUser: AdminId<'user'> = adminId<'user'>('placeholder');
    // @ts-expect-error Admin user identities cannot address lessons.
    const adminLesson: AdminId<'lesson'> = adminUser;
    // @ts-expect-error The admin factory rejects rebranding a different owner.
    adminId<'lesson'>(adminUser);
    // @ts-expect-error Admin dates cannot be rebranded as instants.
    adminUtcInstant(adminCalendarDate('2026-10-08'));
    const catalogSource = decodeCatalogIngredientSource({ productId: 'product', nestedRecipeId: null, amount: 100 });
    if (catalogSource.kind === 'product') {
        // @ts-expect-error Catalog product amounts cannot stand in for recipe servings.
        const catalogServings: CatalogRecipeServings = catalogSource.amount;
        void catalogServings;
    }
    void wrongSelection;
    void wrongAsset;
    void adminLesson;
}
