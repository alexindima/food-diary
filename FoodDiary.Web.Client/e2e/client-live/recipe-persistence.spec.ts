import { randomUUID } from 'node:crypto';

import type { CreateMealHttpRequest } from '../../src/app/shared/api/sdk/generated/model/create-meal-http-request';
import type { CreateProductHttpRequest } from '../../src/app/shared/api/sdk/generated/model/create-product-http-request';
import type { CreateRecipeHttpRequest } from '../../src/app/shared/api/sdk/generated/model/create-recipe-http-request';
import type { UpdateRecipeHttpRequest } from '../../src/app/shared/api/sdk/generated/model/update-recipe-http-request';
import { expect, test } from './owned-runtime';
import { HTTP_BAD_REQUEST, HTTP_CREATED, HTTP_NO_CONTENT, wireRecord, wireRecords, wireString } from './wire-values';

const RECIPE_CALORIES = 300;
const INITIAL_MEAL_CALORIES = 225;
const EDITED_MEAL_CALORIES = 75;
const EDITED_SERVINGS = 0.5;

test('recipe grams decode to servings and a used recipe preserves the saved meal snapshot', async ({ page, request, owned }) => {
    const headers = owned.headers;
    const productResponse = await request.post(`${owned.frontendUrl}/api/v1/products`, {
        headers,
        data: {
            name: `Live recipe ingredient ${randomUUID()}`,
            productType: 'Grain',
            baseUnit: 'G',
            baseAmount: 100,
            defaultPortionAmount: 100,
            caloriesPerBase: 120,
            proteinsPerBase: 10,
            fatsPerBase: 5,
            carbsPerBase: 20,
            fiberPerBase: 3,
            alcoholPerBase: 0,
            visibility: 'Private',
        } satisfies CreateProductHttpRequest,
    });
    expect(productResponse.status()).toBe(HTTP_CREATED);
    const productId = wireString(wireRecord(await productResponse.json())['id']);
    const recipeData = {
        name: `Live portion recipe ${randomUUID()}`,
        servings: 2,
        visibility: 'Private',
        calculateNutritionAutomatically: true,
        steps: [{ description: 'Mix the owned ingredient', ingredients: [{ productId, amount: 250 }] }],
    } satisfies CreateRecipeHttpRequest;
    const recipeResponse = await request.post(`${owned.frontendUrl}/api/v1/recipes`, { headers, data: recipeData });
    expect(recipeResponse.status()).toBe(HTTP_CREATED);
    const recipe = wireRecord(await recipeResponse.json());
    const recipeId = wireString(recipe['id']);
    expect(recipe['totalCalories']).toBe(RECIPE_CALORIES);
    const mealResponse = await request.post(`${owned.frontendUrl}/api/v1/meals`, {
        headers,
        data: {
            date: new Date().toISOString(),
            mealType: 'Lunch',
            isNutritionAutoCalculated: true,
            items: [{ recipeId, amount: 1.5, origin: 'Manual' }],
        } satisfies CreateMealHttpRequest,
    });
    expect(mealResponse.status()).toBe(HTTP_CREATED);
    const meal = wireRecord(await mealResponse.json());
    const mealId = wireString(meal['id']);
    expect(meal['totalCalories']).toBe(INITIAL_MEAL_CALORIES);
    try {
        await page.goto(`${owned.frontendUrl}/meals/${mealId}/edit`);
        await page.getByRole('button', { name: /Edit manual item/u }).click();
        const dialog = page.getByRole('dialog');
        const amount = dialog.getByRole('spinbutton', { name: /Amount/u });
        // The complete gram-only recipe weighs 250 g and yields two 125 g portions.
        await expect(amount).toHaveValue('187.5');
        await amount.fill('62.5');
        await dialog.getByRole('button', { name: 'Add to meal', exact: true }).click();
        await expect(dialog).toHaveCount(0);
        await expect(page.getByRole('textbox', { name: 'Calories, kcal', exact: true })).toHaveValue('75');
        await page.locator('fd-meal-nutrition-sidebar').getByRole('button', { name: 'Save', exact: true }).click();
        await expect(page).toHaveURL(/\/meals$/u);
        const persisted = await request.get(`${owned.frontendUrl}/api/v1/meals/${mealId}`, { headers });
        const stored = wireRecord(await persisted.json());
        expect(stored['totalCalories']).toBe(EDITED_MEAL_CALORIES);
        expect(wireRecords(stored['items'])[0]?.['amount']).toBe(EDITED_SERVINGS);
        // The existing owner policy rejects edits to a recipe already used in a meal.
        const rejected = await request.patch(`${owned.frontendUrl}/api/v1/recipes/${recipeId}`, {
            headers,
            data: { ...recipeData, servings: 4 } satisfies UpdateRecipeHttpRequest,
        });
        expect(rejected.status()).toBe(HTTP_BAD_REQUEST);
        await page.goto(`${owned.frontendUrl}/meals/${mealId}/edit`);
        await page.reload();
        await expect(page.getByRole('textbox', { name: 'Calories, kcal', exact: true })).toHaveValue('75');
    } finally {
        const deletedMeal = await request.delete(`${owned.frontendUrl}/api/v1/meals/${mealId}`, { headers });
        const deletedRecipe = await request.delete(`${owned.frontendUrl}/api/v1/recipes/${recipeId}`, { headers });
        const deletedProduct = await request.delete(`${owned.frontendUrl}/api/v1/products/${productId}`, { headers });
        expect(deletedMeal.status()).toBe(HTTP_NO_CONTENT);
        expect(deletedRecipe.status()).toBe(HTTP_NO_CONTENT);
        expect(deletedProduct.status()).toBe(HTTP_NO_CONTENT);
    }
});
