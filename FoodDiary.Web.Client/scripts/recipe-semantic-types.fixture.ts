import {
    displayRecipeServings,
    recipeDisplayToServings,
    UNKNOWN_SERVING_MASS,
} from '../src/app/features/meals/lib/recipe-serving/recipe-display-amount';
import type { RecipeIngredient } from '../src/app/shared/models/recipe-ingredient';
import { entityId } from '../src/app/shared/models/semantics/entity-id';
import { productQuantity, recipeServings } from '../src/app/shared/models/semantics/meal-quantity';
import {
    nestedRecipeIngredientServingsFromStored,
    productIngredientAmountFromStored,
    recipeDisplayGramsFromInput,
    recipeServingMass,
} from '../src/app/shared/models/semantics/recipe-quantity';

const product: Extract<RecipeIngredient, { kind: 'product' }> = {
    id: entityId<'recipe-ingredient'>('i'),
    kind: 'product',
    productId: entityId<'product'>('p'),
    amount: productIngredientAmountFromStored(1),
};
const nested: Extract<RecipeIngredient, { kind: 'recipe' }> = {
    id: entityId<'recipe-ingredient'>('i'),
    kind: 'recipe',
    nestedRecipeId: entityId<'recipe'>('r'),
    amount: nestedRecipeIngredientServingsFromStored(1),
};
// @ts-expect-error Source alternatives cannot be combined in a canonical product ingredient.
product.nestedRecipeId = entityId<'recipe'>('r');
// @ts-expect-error A nested recipe's servings cannot be a product ingredient quantity.
product.amount = nested.amount;
// @ts-expect-error Recipe ownership cannot stand in for a product.
product.productId = entityId<'recipe'>('r');
// @ts-expect-error Product quantities cannot stand in for nested recipe servings.
nested.amount = product.amount;
// @ts-expect-error Ingredient quantities cannot silently acquire another source's unit.
nestedRecipeIngredientServingsFromStored(product.amount);
// @ts-expect-error Meal consumption bounds and quantities are a separate role.
productIngredientAmountFromStored(productQuantity(1));

const servings = recipeServings(1);
displayRecipeServings(servings, UNKNOWN_SERVING_MASS);
const grams = recipeDisplayGramsFromInput(1);
const mass = recipeServingMass(1);
recipeDisplayToServings({ unit: 'grams', value: grams, gramsPerServing: mass });
// @ts-expect-error Displayed grams cannot stand in for consumed servings.
displayRecipeServings(grams, UNKNOWN_SERVING_MASS);
// @ts-expect-error A grams quantity requires its known mass; unknown fallback must use servings.
recipeDisplayToServings({ unit: 'grams', value: grams });
// @ts-expect-error A grams quantity cannot be labeled servings.
recipeDisplayToServings({ unit: 'servings', value: grams });
// @ts-expect-error A serving quantity cannot silently acquire grams meaning.
recipeDisplayGramsFromInput(servings);
// @ts-expect-error Mass is grams per serving, distinct from displayed grams.
recipeDisplayToServings({ unit: 'grams', value: mass, gramsPerServing: grams });
void product;
void nested;
