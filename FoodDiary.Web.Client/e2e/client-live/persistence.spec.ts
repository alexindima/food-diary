import { randomUUID } from 'node:crypto';

import type { CreateMealHttpRequest } from '../../src/app/shared/api/sdk/generated/model/create-meal-http-request';
import type { CreateProductHttpRequest } from '../../src/app/shared/api/sdk/generated/model/create-product-http-request';
import { expect, test } from './owned-runtime';
import { HTTP_CREATED, HTTP_NO_CONTENT, HTTP_OK, wireRecord, wireRecords, wireString } from './wire-values';

const WATER_AMOUNT_ML = 250;
const EDITED_AMOUNT_G = 175.5;
const EXPECTED_CALORIES = 210.6;

test('water added in the dashboard survives reload and appears in the real API', async ({ page, request, owned }) => {
    const headers = { authorization: `Bearer ${owned.token}` };
    const before = await request.get(`${owned.frontendUrl}/api/v1/hydrations`, { headers });
    expect(before.status()).toBe(HTTP_OK);
    const oldEntries = wireRecords(await before.json());
    await page.goto(`${owned.frontendUrl}/dashboard`);
    const card = page.locator('fd-hydration-card');
    await expect(card).toBeVisible();
    const total = card.locator('.hydration-card__total');
    const totalText = await total.innerText();
    const oldTotal = Number(totalText.replaceAll(/\D/gu, ''));
    const saved = page.waitForResponse(
        response => new URL(response.url()).pathname === '/api/v1/hydrations' && response.request().method() === 'POST',
    );
    await card.getByRole('button', { name: /250/u }).click();
    const savedResponse = await saved;
    expect(savedResponse.status()).toBe(HTTP_CREATED);
    await expect
        .poll(async () => {
            const text = await total.innerText();
            return Number(text.replaceAll(/\D/gu, ''));
        })
        .toBe(oldTotal + WATER_AMOUNT_ML);
    await page.reload();
    await expect
        .poll(async () => {
            const text = await total.innerText();
            return Number(text.replaceAll(/\D/gu, ''));
        })
        .toBe(oldTotal + WATER_AMOUNT_ML);
    const after = await request.get(`${owned.frontendUrl}/api/v1/hydrations`, { headers });
    const entries = wireRecords(await after.json());
    const added = entries.filter(entry => !oldEntries.some(old => old['id'] === entry['id']));
    expect(added).toHaveLength(1);
    expect(added[0]?.['amountMl']).toBe(WATER_AMOUNT_ML);
    // Keep a reused runtime repeatable; remove only the exact entry created here.
    const deleted = await request.delete(`${owned.frontendUrl}/api/v1/hydrations/${wireString(added[0]?.['id'])}`, { headers });
    expect(deleted.status()).toBe(HTTP_NO_CONTENT);
});

test('editing a meal quantity recalculates nutrition and persists through reopen', async ({ page, request, owned }) => {
    const headers = { authorization: `Bearer ${owned.token}` };
    const productResponse = await request.post(`${owned.frontendUrl}/api/v1/products`, {
        headers,
        data: {
            name: `Live quantity ${randomUUID()}`,
            brand: 'Owned fixture',
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
    const mealResponse = await request.post(`${owned.frontendUrl}/api/v1/meals`, {
        headers,
        data: {
            date: new Date().toISOString(),
            mealType: 'Lunch',
            comment: 'Owned live journey',
            isNutritionAutoCalculated: true,
            items: [{ productId, amount: 100, origin: 'Manual' }],
        } satisfies CreateMealHttpRequest,
    });
    expect(mealResponse.status()).toBe(HTTP_CREATED);
    const mealId = wireString(wireRecord(await mealResponse.json())['id']);
    try {
        await page.goto(`${owned.frontendUrl}/meals/${mealId}/edit`);
        await page.getByRole('button', { name: /Edit manual item/u }).click();
        const dialog = page.getByRole('dialog');
        await dialog.getByRole('spinbutton', { name: /Amount/u }).fill('175.5');
        await dialog.getByRole('button', { name: 'Add to meal', exact: true }).click();
        await expect(dialog).toHaveCount(0);
        await expect(page.getByRole('textbox', { name: 'Calories, kcal', exact: true })).toHaveValue('210.6');
        const saved = page.waitForResponse(
            response => new URL(response.url()).pathname === `/api/v1/meals/${mealId}` && response.request().method() === 'PATCH',
        );
        await page.locator('fd-meal-nutrition-sidebar').getByRole('button', { name: 'Save', exact: true }).click();
        const savedResponse = await saved;
        expect(savedResponse.status()).toBe(HTTP_OK);
        await expect(page).toHaveURL(/\/meals$/u);
        const persisted = await request.get(`${owned.frontendUrl}/api/v1/meals/${mealId}`, { headers });
        expect(persisted.status()).toBe(HTTP_OK);
        const value = wireRecord(await persisted.json());
        expect(value['totalCalories']).toBeCloseTo(EXPECTED_CALORIES, 2);
        expect(wireRecords(value['items'])[0]?.['amount']).toBe(EDITED_AMOUNT_G);
        await page.goto(`${owned.frontendUrl}/meals/${mealId}/edit`);
        await page.reload();
        await expect(page.getByRole('textbox', { name: 'Calories, kcal', exact: true })).toHaveValue('210.6');
        await page.getByRole('button', { name: /Edit manual item/u }).click();
        await expect(page.getByRole('dialog').getByRole('spinbutton', { name: /Amount/u })).toHaveValue('175.5');
    } finally {
        const deletedMeal = await request.delete(`${owned.frontendUrl}/api/v1/meals/${mealId}`, { headers });
        const deletedProduct = await request.delete(`${owned.frontendUrl}/api/v1/products/${productId}`, { headers });
        expect(deletedMeal.status()).toBe(HTTP_NO_CONTENT);
        expect(deletedProduct.status()).toBe(HTTP_NO_CONTENT);
    }
});
