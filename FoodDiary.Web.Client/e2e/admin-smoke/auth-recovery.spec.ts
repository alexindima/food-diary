import { readFileSync } from 'node:fs';

import { expect, test } from '@playwright/test';

import { jsonFixture, OpenApiFixtures } from '../support/openapi-fixtures.mjs';

// eslint-disable-next-line security/detect-non-literal-fs-filename -- Fixed repository contract URL without external input.
const adminDocument: unknown = JSON.parse(readFileSync(new URL('../../api-sdk/admin.openapi.json', import.meta.url), 'utf8'));
const contract = new OpenApiFixtures(adminDocument);
const HTTP_UNAVAILABLE = 503;
const HTTP_UNAUTHORIZED = 401;

test('admin startup outage offers retry and a definitive rejection then offers login', async ({ page }) => {
    let unavailable = true;
    let restorations = 0;
    const errors: string[] = [];
    page.on('pageerror', error => errors.push(error.message));
    await page.route('**/api/v1/auth/session', async route => {
        restorations++;
        await route.fulfill({ status: unavailable ? HTTP_UNAVAILABLE : HTTP_UNAUTHORIZED, json: {} });
    });
    await page.goto('/users');
    await expect(page).toHaveURL(/\/unauthorized\?reason=unavailable/u);
    await expect(page.getByRole('heading', { name: 'Could not check your admin session', exact: true })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Go to login', exact: true })).toHaveCount(0);
    await page.screenshot({ path: test.info().outputPath('admin-recovery.png'), fullPage: true });
    unavailable = false;
    await page.getByRole('button', { name: 'Try again', exact: true }).focus();
    await page.keyboard.press('Enter');
    await expect(page.getByText('Your admin session is missing or expired.', { exact: true })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Go to login', exact: true })).toBeVisible();
    expect(restorations).toBe(2);
    expect(errors).toEqual([]);
});

test('admin recovery retries a transient failure and returns to the requested protected route', async ({ page }) => {
    let unavailable = true;
    await page.route('**/api/v1/**', async route => {
        const pathname = new URL(route.request().url()).pathname;
        if (pathname === '/api/v1/auth/session') {
            if (unavailable) {
                await route.fulfill({ status: 503, json: {} });
            } else {
                await route.fulfill(
                    jsonFixture(contract, 'POST', pathname, {
                        sessionId: '00000000-0000-4000-8000-000000000002',
                        roles: ['Admin'],
                    }),
                );
            }
            return;
        }
        await route.fulfill({ status: 503, json: {} });
    });
    await page.goto('/users');
    await expect(page.getByRole('heading', { name: 'Could not check your admin session', exact: true })).toBeVisible();
    unavailable = false;
    await page.getByRole('button', { name: 'Try again', exact: true }).click();
    await expect(page).toHaveURL(/\/users$/u);
    await expect(page.getByRole('heading', { name: 'Accounts', exact: true })).toBeVisible();
});
