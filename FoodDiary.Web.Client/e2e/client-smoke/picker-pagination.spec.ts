import { expect, test } from '@playwright/test';

import { installClientAuthApiAsync } from '../support/auth-flow-fixtures';
import { fixtureUuid, jsonFixture, OpenApiFixtures } from '../support/openapi-fixtures.mjs';

const contract = new OpenApiFixtures();
const TOTAL_PAGES = 3;

function productPage(page: number, limit: number): object {
    return {
        data: [
            {
                id: fixtureUuid(`picker-product-${page}`),
                name: `Product on page ${page}`,
                baseUnit: 'G',
                baseAmount: 100,
                defaultPortionAmount: 100,
                caloriesPerBase: 95,
                proteinsPerBase: 10,
                fatsPerBase: 4,
                carbsPerBase: 5,
                fiberPerBase: 0,
                alcoholPerBase: 0,
                usageCount: 0,
                visibility: 'Private',
                createdAt: '2026-04-19T00:00:00Z',
                isOwnedByCurrentUser: true,
                qualityScore: 80,
                qualityGrade: 'green',
            },
        ],
        page,
        limit,
        totalPages: TOTAL_PAGES,
        totalItems: TOTAL_PAGES * limit,
    };
}

for (const scenario of [
    { path: '/meals/add', title: 'Find food and set quantity' },
    { path: '/recipes/add', title: 'Choose an ingredient' },
]) {
    test(`loads product page two in the picker at ${scenario.path}`, async ({ page }) => {
        await page.setViewportSize({ width: 1440, height: 900 });
        const session = await installClientAuthApiAsync(page);
        session.authenticated = true;
        await page.route('**/api/v1/products/overview?*', async route => {
            const limit = Number(new URL(route.request().url()).searchParams.get('limit'));
            await route.fulfill(
                jsonFixture(contract, 'GET', route.request().url(), {
                    allProducts: productPage(1, limit),
                    recentItems: [],
                    favoriteItems: [],
                    favoriteTotalCount: 0,
                }),
            );
        });
        await page.route(/\/api\/v1\/products\?/, async route => {
            const query = new URL(route.request().url()).searchParams;
            await route.fulfill(
                jsonFixture(contract, 'GET', route.request().url(), productPage(Number(query.get('page')), Number(query.get('limit')))),
            );
        });
        await page.goto(scenario.path);
        if (scenario.path === '/recipes/add') {
            await page.getByRole('button', { name: 'Add ingredient', exact: true }).click();
            await page.getByRole('textbox', { name: 'Select ingredient', exact: true }).click();
        } else {
            await page.getByRole('button', { name: 'Manually', exact: true }).first().click();
        }
        const picker = page.getByRole('dialog', { name: scenario.title, exact: true });
        await expect(picker.getByText('Product on page 1', { exact: true })).toBeVisible();
        await picker.getByRole('button', { name: '2', exact: true }).click();
        await expect(picker.getByText('Product on page 2', { exact: true }), 'Picker did not display the requested page').toBeVisible();
        await expect(picker.getByRole('button', { name: '2', exact: true })).toHaveAttribute('aria-current', 'page');
    });
}
