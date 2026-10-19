import { inject, Injector, Service } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import type { ProductService } from '../../features/products/api/product.service';
import { RECIPE_MIN_INGREDIENT_AMOUNT } from '../../features/recipes/components/manage/recipe-manage-lib/recipe-manage-form.mapper';
import { RecipeDevelopmentDraft } from '../../features/recipes/contracts/recipe-development-draft';
import { devNumber, devRecord, devText } from '../../shared/development/development-input';
import { DevelopmentToolError } from '../../shared/development/development-tools';
import { entityId } from '../../shared/models/semantics/entity-id';
import { MAXIMUM_CONSUMED_QUANTITY } from '../../shared/models/semantics/meal-quantity';

const CATALOG_LIMIT = 25;
const MAX_DRAFT_STEPS = 10;
const MAX_DRAFT_INGREDIENTS = 20;
const MAX_PAGE = 1000;
const MAX_NAME_LENGTH = 200;
const MAX_SERVINGS = 1000;
const MAX_DESCRIPTION_LENGTH = 5000;
const MAX_ID_LENGTH = 100;
const MAX_UNIT_LENGTH = 10;
const MAX_COOK_TIME = 100_000;

@Service()
export class LocalWebmcpCatalogService {
    private readonly injector = inject(Injector);

    public async searchAsync(kind: 'product' | 'recipe', search: string, rawPage: unknown): Promise<unknown> {
        const page = devNumber(rawPage, 1, MAX_PAGE);
        if (!Number.isInteger(page)) {
            throw new DevelopmentToolError('Page must be an integer.');
        }
        if (kind === 'product') {
            const { ProductService } = await import('../../features/products/api/product.service');
            const result = await firstValueFrom(this.injector.get(ProductService).query(page, CATALOG_LIMIT, { search }));
            return {
                ...result,
                data: result.data.map(product => ({
                    id: product.id,
                    name: product.name,
                    unit: product.baseUnit,
                    baseAmount: product.baseAmount,
                    defaultPortionAmount: product.defaultPortionAmount,
                    caloriesPerBase: product.caloriesPerBase,
                    proteinsPerBase: product.proteinsPerBase,
                    fatsPerBase: product.fatsPerBase,
                    carbsPerBase: product.carbsPerBase,
                })),
            };
        }
        const { RecipeService } = await import('../../features/recipes/api/recipe.service');
        const result = await firstValueFrom(this.injector.get(RecipeService).query(page, CATALOG_LIMIT, { search }));
        return {
            ...result,
            data: result.data.map(recipe => ({
                id: recipe.id,
                name: recipe.name,
                servings: recipe.servings,
                totalCalories: recipe.totalCalories,
            })),
        };
    }

    public async recipeDraftAsync(input: unknown): Promise<RecipeDevelopmentDraft> {
        const value = devRecord(input, ['name', 'servings', 'steps', 'cookTime']);
        const name = devText(value['name'], MAX_NAME_LENGTH, true);
        const servings = devNumber(value['servings'], 1, MAX_SERVINGS);
        if (
            !Number.isInteger(servings) ||
            !Array.isArray(value['steps']) ||
            value['steps'].length === 0 ||
            value['steps'].length > MAX_DRAFT_STEPS
        ) {
            throw new DevelopmentToolError('Expected integer servings and 1–10 steps.');
        }
        const { ProductService } = await import('../../features/products/api/product.service');
        const products = this.injector.get(ProductService);
        const steps: RecipeDevelopmentDraft['steps'] = [];
        for (const rawStep of value['steps']) {
            const step = devRecord(rawStep, ['description', 'ingredients']);
            const description = devText(step['description'], MAX_DESCRIPTION_LENGTH, true);
            if (!Array.isArray(step['ingredients']) || step['ingredients'].length > MAX_DRAFT_INGREDIENTS) {
                throw new DevelopmentToolError('Too many ingredients.');
            }
            const ingredients = await this.resolveIngredientsAsync(step['ingredients'], products);
            steps.push({ description, ingredients });
        }
        return new RecipeDevelopmentDraft(name, servings, steps, devNumber(value['cookTime'] ?? 1, 1, MAX_COOK_TIME));
    }

    private async resolveIngredientsAsync(
        raw: unknown[],
        products: ProductService,
    ): Promise<RecipeDevelopmentDraft['steps'][number]['ingredients']> {
        const ingredients: RecipeDevelopmentDraft['steps'][number]['ingredients'] = [];
        for (const rawIngredient of raw) {
            const item = devRecord(rawIngredient, ['productId', 'recipeId', 'amount', 'unit']);
            if (item['recipeId'] !== undefined) {
                if (item['productId'] !== undefined || item['unit'] !== 'servings') {
                    throw new DevelopmentToolError('A nested recipe requires only recipeId, amount and unit=servings.');
                }
                const { RecipeService } = await import('../../features/recipes/api/recipe.service');
                const recipe = await firstValueFrom(
                    this.injector.get(RecipeService).getById(entityId<'recipe'>(devText(item['recipeId'], MAX_ID_LENGTH))),
                );
                const amount = devNumber(item['amount'], RECIPE_MIN_INGREDIENT_AMOUNT, MAXIMUM_CONSUMED_QUANTITY);
                if (recipe === null || !Number.isFinite(recipe.servings) || recipe.servings <= 0) {
                    throw new DevelopmentToolError('Nested recipe missing or unavailable.');
                }
                ingredients.push({ recipe, amount });
                continue;
            }
            const id = devText(item['productId'], MAX_ID_LENGTH);
            const amount = devNumber(item['amount'], RECIPE_MIN_INGREDIENT_AMOUNT, MAXIMUM_CONSUMED_QUANTITY);
            const unit = devText(item['unit'], MAX_UNIT_LENGTH);
            const product = await firstValueFrom(products.getById(entityId<'product'>(id)));
            if (product === null || String(product.baseUnit) !== unit) {
                throw new DevelopmentToolError('Product missing, unavailable or unit mismatch.');
            }
            ingredients.push({ product, amount });
        }
        return ingredients;
    }
}
