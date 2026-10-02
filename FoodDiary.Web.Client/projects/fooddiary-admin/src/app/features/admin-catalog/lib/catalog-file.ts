import type { CatalogFile, CatalogIngredient, CatalogProduct, CatalogRecipe, CatalogStep } from '../models/catalog-transfer';

export const MAX_CATALOG_FILE_BYTES = 5_242_880;
const MAX_ITEMS = 5000;
const GUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
const EMPTY_GUID = '00000000-0000-0000-0000-000000000000';

function record(value: unknown): value is Record<string, unknown> {
    return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function identifier(value: unknown): value is string {
    return typeof value === 'string' && GUID.test(value) && value !== EMPTY_GUID;
}

function strings(value: Record<string, unknown>, required: string[], optional: string[]): boolean {
    return (
        required.every(key => typeof value[key] === 'string') &&
        optional.every(key => nullish(value[key]) || typeof value[key] === 'string')
    );
}

function numbers(value: Record<string, unknown>, required: string[], optional: string[] = []): boolean {
    return (
        required.every(key => typeof value[key] === 'number' && Number.isFinite(value[key])) &&
        optional.every(key => nullish(value[key]) || (typeof value[key] === 'number' && Number.isFinite(value[key])))
    );
}

function product(value: unknown): value is CatalogProduct {
    return (
        record(value) &&
        identifier(value['id']) &&
        strings(value, ['name', 'productType', 'baseUnit'], ['barcode', 'brand', 'category', 'description', 'imageUrl']) &&
        numbers(value, [
            'baseAmount',
            'defaultPortionAmount',
            'caloriesPerBase',
            'proteinsPerBase',
            'fatsPerBase',
            'carbsPerBase',
            'fiberPerBase',
            'alcoholPerBase',
        ])
    );
}

function ingredient(value: unknown): value is CatalogIngredient {
    return (
        record(value) &&
        numbers(value, ['amount']) &&
        strings(value, [], ['textName', 'amountText']) &&
        (nullish(value['productId']) || identifier(value['productId'])) &&
        (nullish(value['nestedRecipeId']) || identifier(value['nestedRecipeId']))
    );
}

function step(value: unknown): value is CatalogStep {
    return (
        record(value) &&
        strings(value, ['description'], ['title', 'imageUrl']) &&
        numbers(value, ['order']) &&
        Array.isArray(value['ingredients']) &&
        value['ingredients'].every(ingredient) &&
        nullish(value['imageAssetId']) &&
        (nullish(value['imageAssetIds']) || (Array.isArray(value['imageAssetIds']) && value['imageAssetIds'].length === 0))
    );
}

function recipe(value: unknown): value is CatalogRecipe {
    return (
        record(value) &&
        identifier(value['id']) &&
        strings(value, ['name', 'language'], ['description', 'category', 'imageUrl']) &&
        numbers(
            value,
            ['servings'],
            ['prepTime', 'cookTime', 'manualCalories', 'manualProteins', 'manualFats', 'manualCarbs', 'manualFiber', 'manualAlcohol'],
        ) &&
        typeof value['languageConfirmed'] === 'boolean' &&
        typeof value['calculateNutritionAutomatically'] === 'boolean' &&
        Array.isArray(value['steps']) &&
        value['steps'].every(step)
    );
}

function nullish(value: unknown): value is null | undefined {
    return value === null || value === undefined;
}

function catalog(value: unknown): value is CatalogFile {
    return (
        record(value) &&
        value['format'] === 'fooddiary-catalog' &&
        value['version'] === 1 &&
        Array.isArray(value['products']) &&
        Array.isArray(value['recipes']) &&
        value['products'].length <= MAX_ITEMS &&
        value['recipes'].length <= MAX_ITEMS &&
        value['products'].every(product) &&
        value['recipes'].every(recipe)
    );
}
export function parseCatalogFile(text: string): CatalogFile {
    const value: unknown = JSON.parse(text);
    if (!catalog(value)) {
        throw new Error('ADMIN_CATALOG_TRANSFER.INVALID_FILE');
    }
    const file = value;
    for (const items of [file.products, file.recipes]) {
        const ids = items.map(item => item.id.toLowerCase());
        if (new Set(ids).size !== ids.length) {
            throw new Error('ADMIN_CATALOG_TRANSFER.DUPLICATE_IDS');
        }
    }
    // GUID identity is case-insensitive on the server; normalize before resolving links.
    return {
        format: 'fooddiary-catalog',
        version: 1,
        products: file.products.map(item => ({ ...item, id: item.id.toLowerCase() })),
        recipes: file.recipes.map(item => ({
            ...item,
            id: item.id.toLowerCase(),
            steps: item.steps.map(itemStep => ({
                ...itemStep,
                ingredients: itemStep.ingredients.map(itemIngredient => ({
                    ...itemIngredient,
                    productId: itemIngredient.productId?.toLowerCase() ?? null,
                    nestedRecipeId: itemIngredient.nestedRecipeId?.toLowerCase() ?? null,
                })),
            })),
        })),
    };
}

export function orderCatalogRecipes(recipes: CatalogRecipe[], existingIds: Set<string>): CatalogRecipe[] {
    const pending = new Map(recipes.filter(item => !existingIds.has(item.id)).map(item => [item.id, item]));
    const resolved = new Set(existingIds);
    const ordered: CatalogRecipe[] = [];
    while (pending.size > 0) {
        const ready = [...pending.values()].filter(item =>
            item.steps.every(itemStep =>
                itemStep.ingredients.every(
                    itemIngredient => itemIngredient.nestedRecipeId === null || resolved.has(itemIngredient.nestedRecipeId),
                ),
            ),
        );
        if (ready.length === 0) {
            throw new Error('ADMIN_CATALOG_TRANSFER.RECIPE_LINKS');
        }
        for (const item of ready) {
            ordered.push(item);
            resolved.add(item.id);
            pending.delete(item.id);
        }
    }
    return [...recipes.filter(item => existingIds.has(item.id)), ...ordered];
}
