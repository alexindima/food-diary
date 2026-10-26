import { randomUUID } from 'node:crypto';
import { readFile } from 'node:fs/promises';

import type { Page } from '@playwright/test';

import type { AdminUserCreateHttpRequest } from '../../projects/fooddiary-admin/src/app/shared/api/sdk/generated/model/admin-user-create-http-request';
import type { CreateMealHttpRequest } from '../../src/app/shared/api/sdk/generated/model/create-meal-http-request';
import type { CreateProductHttpRequest } from '../../src/app/shared/api/sdk/generated/model/create-product-http-request';
import { expect, readSessionCookie, test } from './owned-runtime';
import { HTTP_CREATED, HTTP_NO_CONTENT, HTTP_OK, wireRecord, wireRecords, wireString } from './wire-values';

const FILTER_DAY = 15;
const ADJACENT_DAY = 16;
const PDF_HEADER_BYTES = 5;
const HTTP_UNAUTHORIZED = 401;

async function assertPdfExportAsync(page: Page, frontendUrl: string): Promise<void> {
    await page.goto(`${frontendUrl}/statistics`);
    await page.getByRole('button', { name: 'Export', exact: true }).click();
    const download = page.waitForEvent('download');
    const pdf = page.waitForResponse(value => new URL(value.url()).pathname === '/api/v1/export/diary', { timeout: 15_000 });
    await page.getByRole('menuitem', { name: /PDF/u }).click();
    const pdfResponse = await pdf;
    expect(pdfResponse.status()).toBe(HTTP_OK);
    const exported = await download;
    expect(exported.suggestedFilename()).toMatch(/\.pdf$/u);
    const path = await exported.path();
    // eslint-disable-next-line security/detect-non-literal-fs-filename -- Playwright supplied this exact owned download path after the verified loopback API response.
    const bytes = await readFile(path);
    expect(bytes.subarray(0, PDF_HEADER_BYTES).toString()).toBe('%PDF-');
}

test('ordinary-user login, cookie refresh and reload reach the same owned profile', async ({ browser, request, owned }) => {
    const email = `journey-${randomUUID()}@example.test`;
    const password = owned.credentials.password;
    const created = await request.post(`${owned.frontendUrl}/api/v1/admin/users`, {
        headers: owned.headers,
        data: {
            email,
            firstName: 'Owned',
            lastName: 'Journey',
            language: 'en',
            roles: [],
            temporaryPassword: password,
            generatePassword: false,
            isEmailConfirmed: true,
            sendCredentialsEmail: false,
            requirePasswordChange: false,
        } satisfies AdminUserCreateHttpRequest,
    });
    expect(created.status()).toBe(HTTP_CREATED);
    const userId = wireString(wireRecord(wireRecord(await created.json())['user'])['id']);
    const context = await browser.newContext({ baseURL: owned.frontendUrl, timezoneId: 'UTC', viewport: { width: 390, height: 844 } });
    const page = await context.newPage();
    try {
        await page.goto('/mobile/login');
        await expect(page.getByRole('tab', { name: 'Login', exact: true })).toHaveAttribute('aria-selected', 'true');
        await page.getByLabel('Email', { exact: true }).fill(email);
        await page.getByLabel('Password', { exact: true }).fill(password);
        await page.getByRole('button', { name: 'Login', exact: true }).click();
        await expect(page).toHaveURL(/\/dashboard/u);
        const restored = await context.request.post('/api/v1/auth/session', {
            headers: { origin: owned.frontendUrl, 'X-Requested-With': 'XMLHttpRequest' },
        });
        expect(restored.status()).toBe(HTTP_OK);
        const metadata = wireRecord(await restored.json());
        expect(metadata['accessToken']).toBeUndefined();
        expect(metadata['refreshToken']).toBeUndefined();
        expect(wireString(metadata['sessionId'])).toBeTruthy();
        await page.reload();
        await expect(page.locator('fd-dashboard')).toBeVisible();
        expect(await page.evaluate(() => localStorage.getItem('authToken'))).toBeNull();
        expect(await page.evaluate(() => sessionStorage.getItem('authToken'))).toBeNull();
        const profile = await context.request.get('/api/v1/users/info');
        expect(profile.status()).toBe(HTTP_OK);
        expect(wireRecord(await profile.json())['id']).toBe(userId);
        await page.reload();
        await expect(page.locator('fd-dashboard')).toBeVisible();
    } finally {
        await context.request.delete('/api/v1/users', { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
        await context.close();
    }
});

test('meal date filtering excludes the adjacent day and exports a real PDF', async ({ page, request, owned }) => {
    const headers = owned.headers;
    const marker = `Owned calendar ${randomUUID()}`;
    const ids: string[] = [];
    const productIds: string[] = [];
    try {
        for (const day of [FILTER_DAY, ADJACENT_DAY]) {
            const product = await request.post(`${owned.frontendUrl}/api/v1/products`, {
                headers,
                data: {
                    name: `${marker} ${day}`,
                    productType: 'Grain',
                    baseUnit: 'G',
                    baseAmount: 100,
                    defaultPortionAmount: 100,
                    caloriesPerBase: 120,
                    visibility: 'Private',
                } satisfies CreateProductHttpRequest,
            });
            expect(product.status()).toBe(HTTP_CREATED);
            const productId = wireString(wireRecord(await product.json())['id']);
            productIds.push(productId);
            const response = await request.post(`${owned.frontendUrl}/api/v1/meals`, {
                headers,
                data: {
                    date: `2026-09-${day}T12:00:00Z`,
                    mealType: 'Lunch',
                    comment: `${marker} ${day}`,
                    isNutritionAutoCalculated: true,
                    items: [{ productId, amount: 100, origin: 'Manual' }],
                } satisfies CreateMealHttpRequest,
            });
            expect(response.status()).toBe(HTTP_CREATED);
            ids.push(wireString(wireRecord(await response.json())['id']));
        }
        await page.goto(`${owned.frontendUrl}/meals`);
        await page.getByRole('button', { name: 'Date filter', exact: true }).click();
        const dialog = page.getByRole('dialog');
        for (const label of ['Date from', 'Date to']) {
            await dialog.getByRole('combobox', { name: label, exact: true }).click();
            const day = page.getByRole('gridcell', { name: 'September 15, 2026', exact: true });
            for (let month = 0; month < 2 && (await day.count()) === 0; month++) {
                const title = page.locator('fd-ui-calendar .fd-ui-calendar__title');
                const oldTitle = await title.innerText();
                await page.getByRole('button', { name: 'Previous month', exact: true }).click();
                await expect(title).not.toHaveText(oldTitle);
            }
            await day.click();
        }
        const filtered = page.waitForResponse(response => new URL(response.url()).pathname === '/api/v1/meals/overview', {
            timeout: 15_000,
        });
        await dialog.getByRole('button', { name: 'Apply', exact: true }).click();
        const response = await filtered;
        expect(response.status()).toBe(HTTP_OK);
        const result = wireRecord(await response.json());
        const meals = wireRecords(wireRecord(result['allMeals'])['data']);
        expect(meals.some(item => item['id'] === ids[0])).toBe(true);
        expect(meals.some(item => item['id'] === ids[1])).toBe(false);
        await expect(page.locator('.entity-card__comment').getByText(`${marker} 15`, { exact: true })).toBeVisible();
        await expect(page.getByText(`${marker} 16`, { exact: true })).toHaveCount(0);
        await assertPdfExportAsync(page, owned.frontendUrl);
    } finally {
        for (const id of ids) {
            const deleted = await request.delete(`${owned.frontendUrl}/api/v1/meals/${id}`, { headers });
            expect(deleted.status()).toBe(HTTP_NO_CONTENT);
        }
        for (const id of productIds) {
            const deleted = await request.delete(`${owned.frontendUrl}/api/v1/products/${id}`, { headers });
            expect(deleted.status()).toBe(HTTP_NO_CONTENT);
        }
    }
});

test('pasted recipe text creates a persisted draft without an external AI call', async ({ page, request, owned }) => {
    const marker = `Owned imported recipe ${randomUUID()}`;
    await page.goto(`${owned.frontendUrl}/recipes/add`);
    await page.locator('details.recipe-manage__import-card > summary').click();
    await page.getByLabel('Recipe text', { exact: true }).fill(`${marker}\nIngredients: rice 100 g\nSteps: mix and serve`);
    await page.getByRole('button', { name: 'Use text without AI', exact: true }).click();
    const saved = page.waitForResponse(
        response => new URL(response.url()).pathname === '/api/v1/recipes' && response.request().method() === 'POST',
    );
    await page.getByRole('button', { name: 'Create recipe', exact: true }).click();
    const response = await saved;
    expect(response.status()).toBe(HTTP_CREATED);
    const recipe = wireRecord(await response.json());
    const id = wireString(recipe['id']);
    try {
        await page.goto(`${owned.frontendUrl}/recipes/${id}/edit`);
        await page.reload();
        await expect(page.getByRole('textbox', { name: 'Name', exact: true })).toHaveValue(marker);
        const read = await request.get(`${owned.frontendUrl}/api/v1/recipes/${id}`, {
            headers: owned.headers,
        });
        expect(read.status()).toBe(HTTP_OK);
        expect(wireRecord(await read.json())['name']).toBe(marker);
    } finally {
        const deleted = await request.delete(`${owned.frontendUrl}/api/v1/recipes/${id}`, {
            headers: owned.headers,
        });
        expect(deleted.status()).toBe(HTTP_NO_CONTENT);
    }
});

test('ordinary-user account deletion schedules removal and revokes existing access', async ({ browser, request, owned }) => {
    const email = `journey-delete-${randomUUID()}@example.test`;
    const created = await request.post(`${owned.frontendUrl}/api/v1/admin/users`, {
        headers: owned.headers,
        data: {
            email,
            firstName: 'Owned',
            lastName: 'Deletion',
            language: 'en',
            roles: [],
            temporaryPassword: owned.credentials.password,
            generatePassword: false,
            isEmailConfirmed: true,
            sendCredentialsEmail: false,
            requirePasswordChange: false,
        } satisfies AdminUserCreateHttpRequest,
    });
    expect(created.status()).toBe(HTTP_CREATED);
    const context = await browser.newContext({ baseURL: owned.frontendUrl, viewport: { width: 390, height: 844 } });
    const login = await context.request.post('/api/v1/auth/login', {
        data: { email, password: owned.credentials.password },
        headers: { 'X-Requested-With': 'XMLHttpRequest' },
    });
    expect(login.status()).toBe(HTTP_OK);
    const oldCookie = readSessionCookie(login);
    const page = await context.newPage();
    try {
        await page.goto('/profile');
        await page
            .locator('details')
            .filter({ has: page.locator('fd-user-manage-privacy-card') })
            .locator('summary')
            .click();
        await page.locator('fd-user-manage-privacy-card').getByRole('button', { name: 'Delete', exact: true }).click();
        const deleted = page.waitForResponse(
            response => new URL(response.url()).pathname === '/api/v1/users' && response.request().method() === 'DELETE',
        );
        await page.getByRole('dialog').getByRole('button', { name: 'Delete account', exact: true }).click();
        const deletedResponse = await deleted;
        expect(deletedResponse.status()).toBe(HTTP_NO_CONTENT);
        await expect(page).toHaveURL(/\/(?:mobile\/login|\?auth=login)?$/u);
        const oldAccess = await request.get(`${owned.frontendUrl}/api/v1/users/info`, {
            headers: { Cookie: oldCookie },
        });
        expect(oldAccess.status()).toBe(HTTP_UNAUTHORIZED);
    } finally {
        await context.close();
    }
});
