import { expect, type Locator, type Page, test } from '@playwright/test';

import type { MealAiItemHttpResponse } from '../../src/app/shared/api/sdk/generated/model/meal-ai-item-http-response';
import type { MealHttpResponse } from '../../src/app/shared/api/sdk/generated/model/meal-http-response';
import { consentingPrivacyFixture, userFixture } from '../../src/testing/api-fixtures';
import { installClientAuthApiAsync } from '../support/auth-flow-fixtures';
import { fixtureUuid, jsonFixture, OpenApiFixtures } from '../support/openapi-fixtures.mjs';

const contract = new OpenApiFixtures();
const RECOGNIZED_AT = '2026-11-05T12:00:00.000Z';
const MEAL_ID = fixtureUuid('recognition-drag-meal');
const SESSION_ID = fixtureUuid('recognition-drag-session');
const PORTIONS = [
    { name: 'Apple', amount: 120, calories: 62 },
    { name: 'Banana', amount: 85, calories: 76 },
    { name: 'Carrot', amount: 60, calories: 25 },
] as const;
const ORIGINAL_ORDER = ['Apple', 'Banana', 'Carrot'];
const REORDERED = ['Carrot', 'Apple', 'Banana'];
const REORDERED_AMOUNTS = ['60', '120', '85'];
const NUTRIENTS = { protein: 0, fat: 0, carbs: 0, fiber: 0, alcohol: 0 };
const DRAG_ACTIVATION_PIXELS = 12;
const DROP_UPPER_FRACTION = 0.25;

test.use({ serviceWorkers: 'block', viewport: { width: 1440, height: 1080 } });

test('reorders the shared recognition result by dragging and applying the draft', async ({ page, baseURL }) => {
    const unexpected = await authenticateRecognitionOrderAsync(page, baseURL);
    let nutritionRequests = 0;
    const query = '120 g apple, 85 g banana, 60 g carrot';
    await page.route('**/api/v1/ai/food/text', async route => {
        const request = route.request();
        expect(request.postDataJSON()).toEqual({ text: query });
        await route.fulfill(
            jsonFixture(contract, 'POST', request.url(), {
                items: PORTIONS.map(portion => ({ nameEn: portion.name, amount: portion.amount, unit: 'g', confidence: 1 })),
            }),
        );
    });
    await page.route('**/api/v1/ai/food/nutrition', async route => {
        nutritionRequests++;
        await route.fulfill(
            jsonFixture(contract, 'POST', route.request().url(), {
                calories: PORTIONS.reduce((sum, portion) => sum + portion.calories, 0),
                ...NUTRIENTS,
                items: PORTIONS.map(portion => ({ ...portion, ...NUTRIENTS, unit: 'g' })),
            }),
        );
    });

    await page.goto('/meals/add');
    const input = page.getByRole('textbox', { name: /^Describe your meal/u });
    await input.fill(query);
    await input.press('Enter');
    const result = page.locator('fd-ai-photo-result');
    const names = result.locator('.ai-photo-result__item-name');
    await expect(names).toHaveText(ORIGINAL_ORDER);
    await result.getByRole('button', { name: 'Edit', exact: true }).click();
    const rows = result.locator('.ai-photo-result__edit-row');
    await expectDraftOrderAsync(rows, ORIGINAL_ORDER);

    await dragRecognitionItemAsync(page, rows.first(), rows.first());
    await expectDraftOrderAsync(rows, ORIGINAL_ORDER);
    await dragRecognitionItemAsync(page, rows.last(), rows.first());
    await expectDraftOrderAsync(rows, REORDERED, REORDERED_AMOUNTS);
    await result.getByRole('button', { name: 'Save', exact: true }).click();

    await expect(names).toHaveText(REORDERED);
    expect(nutritionRequests).toBe(1);
    expect(unexpected()).toEqual([]);
});

test('keeps a dragged order after saving and reopening a meal recognition', async ({ page, baseURL }) => {
    const unexpected = await authenticateRecognitionOrderAsync(page, baseURL);
    await page.route(`**/api/v1/meals/${MEAL_ID}`, async route => {
        await route.fulfill(jsonFixture(contract, 'GET', route.request().url(), savedMealRecognition()));
    });
    await page.goto(`/meals/${MEAL_ID}/edit`);

    const editRecognition = page.getByRole('button', { name: 'Edit recognition', exact: true });
    await editRecognition.click();
    const dialog = page.locator('fd-meal-photo-recognition-dialog');
    const names = dialog.locator('.photo-ai-dialog__table-body .photo-ai-dialog__cell--name');
    await expect(names).toHaveText(ORIGINAL_ORDER);
    await dialog.getByRole('button', { name: 'Edit', exact: true }).click();
    const rows = dialog.locator('.photo-ai-dialog__edit-row');
    await expectDraftOrderAsync(rows, ORIGINAL_ORDER);

    await dragRecognitionItemAsync(page, rows.last(), rows.first());
    await expectDraftOrderAsync(rows, REORDERED, REORDERED_AMOUNTS);
    await dialog.getByRole('button', { name: 'Save', exact: true }).click();
    await expect(names).toHaveText(REORDERED);
    await dialog.getByRole('button', { name: 'Save', exact: true }).click();

    await expect(dialog).toHaveCount(0);
    await editRecognition.click();
    await expect(names).toHaveText(REORDERED);
    expect(unexpected()).toEqual([]);
});

async function authenticateRecognitionOrderAsync(page: Page, baseURL: string | undefined): Promise<() => string[]> {
    if (baseURL === undefined) {
        throw new Error('The recognition smoke test requires the local client base URL.');
    }
    const origin = new URL(baseURL).origin;
    const externalRequests: string[] = [];
    await page.route('**/*', async route => {
        const url = new URL(route.request().url());
        if (url.origin === origin) {
            await route.continue();
            return;
        }
        externalRequests.push(url.origin);
        await route.abort('blockedbyclient');
    });
    const user = userFixture({ aiConsentAcceptedAt: RECOGNIZED_AT });
    const api = await installClientAuthApiAsync(page, user);
    await page.route('**/api/v1/auth/session', async route => {
        await route.fulfill(
            jsonFixture(contract, 'POST', route.request().url(), { sessionId: SESSION_ID, roles: ['User', 'Premium'], user }),
        );
    });
    await page.route('**/api/v1/users/privacy', async route => {
        await route.fulfill(
            jsonFixture(contract, 'GET', route.request().url(), { ...consentingPrivacyFixture(), aiAcceptedAtUtc: RECOGNIZED_AT }),
        );
    });
    return () => [...externalRequests, ...api.unexpectedRequests];
}

async function expectDraftOrderAsync(rows: Locator, names: string[], amounts?: string[]): Promise<void> {
    await expect(rows).toHaveCount(names.length);
    for (const [index, name] of names.entries()) {
        await expect(rows.nth(index).getByRole('textbox')).toHaveValue(name);
        if (amounts !== undefined) {
            await expect(rows.nth(index).getByRole('spinbutton')).toHaveValue(amounts[index]);
        }
    }
}

async function dragRecognitionItemAsync(page: Page, sourceRow: Locator, destinationRow: Locator): Promise<void> {
    const handle = sourceRow.locator('.cdk-drag-handle');
    await expect(handle).toBeVisible();
    await handle.scrollIntoViewIfNeeded();
    const source = await handle.boundingBox();
    const target = await destinationRow.boundingBox();
    if (source === null || target === null) {
        throw new Error('The drag handle and destination recognition row must be visible.');
    }
    const x = source.x + source.width / 2;
    const y = source.y + source.height / 2;
    await page.mouse.move(x, y);
    await page.mouse.down();
    await page.mouse.move(x, y - DRAG_ACTIVATION_PIXELS, { steps: 3 });
    await expect(page.locator('.cdk-drag-preview')).toBeVisible();
    await page.mouse.move(x, target.y + target.height * DROP_UPPER_FRACTION, { steps: 15 });
    await page.mouse.up();
    await expect(page.locator('.cdk-drag-preview')).toHaveCount(0);
}

function savedMealRecognition(): MealHttpResponse {
    const items: MealAiItemHttpResponse[] = PORTIONS.map(portion => ({
        id: fixtureUuid(`recognition-drag-${portion.name}`),
        sessionId: SESSION_ID,
        nameEn: portion.name,
        amount: portion.amount,
        unit: 'g',
        confidence: 1,
        resolution: 'Accepted',
        calories: portion.calories,
        proteins: 0,
        fats: 0,
        carbs: 0,
        fiber: 0,
        alcohol: 0,
    }));
    return {
        id: MEAL_ID,
        date: RECOGNIZED_AT,
        mealType: 'Lunch',
        totalCalories: PORTIONS.reduce((sum, portion) => sum + portion.calories, 0),
        totalProteins: 0,
        totalFats: 0,
        totalCarbs: 0,
        totalFiber: 0,
        totalAlcohol: 0,
        isNutritionAutoCalculated: true,
        items: [],
        aiSessions: [{ id: SESSION_ID, mealId: MEAL_ID, recognizedAtUtc: RECOGNIZED_AT, source: 'Photo', status: 'Succeeded', items }],
    };
}
