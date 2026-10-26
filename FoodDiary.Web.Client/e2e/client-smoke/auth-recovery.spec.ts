import { expect, test } from '@playwright/test';

import { userFixture } from '../../src/testing/api-fixtures';
import { installClientAuthApiAsync, seedClientSessionAsync } from '../support/auth-flow-fixtures';
import { jsonFixture, OpenApiFixtures } from '../support/openapi-fixtures.mjs';

const contract = new OpenApiFixtures();
const MAX_RESTORATION_REQUESTS = 3;

test.beforeEach(async ({ page, baseURL }) => {
    await page.route('**/*', async route => {
        if (new URL(route.request().url()).origin === baseURL) {
            await route.continue();
        } else {
            await route.abort('blockedbyclient');
        }
    });
});

test('a Russian narrow-screen session outage preserves the requested route and recovers through the retry control', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await page.addInitScript(() => {
        localStorage.setItem('fd_language', 'ru');
    });
    const state = await installClientAuthApiAsync(page, userFixture({ language: 'ru' }));
    await page.route('**/api/v1/logs', async route => route.fulfill({ status: 204 }));
    state.authenticated = true;
    let unavailable = true;
    let restorations = 0;
    await page.route('**/api/v1/auth/session', async route => {
        restorations++;
        if (unavailable) {
            await route.fulfill({ status: 503, json: {} });
        } else {
            await route.fallback();
        }
    });
    const errors: string[] = [];
    page.on('pageerror', error => errors.push(error.message));
    await page.goto('/dashboard');
    const notice = page.locator('fd-auth-recovery-notice');
    await expect(notice.getByText('Нужно подключение', { exact: true })).toBeVisible();
    await expect(page.locator('fd-dashboard')).toHaveCount(0);
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
    await page.screenshot({ path: test.info().outputPath('recovery-ru-mobile.png'), fullPage: true });
    unavailable = false;
    await notice.getByRole('button', { name: 'Повторить', exact: true }).focus();
    await page.keyboard.press('Enter');
    await expect(page).toHaveURL(/\/dashboard$/u);
    await expect(page.locator('fd-dashboard')).toBeVisible();
    await expect(notice).toBeEmpty();
    expect(restorations).toBeGreaterThanOrEqual(2);
    expect(restorations).toBeLessThanOrEqual(MAX_RESTORATION_REQUESTS);
    expect(state.unexpectedRequests).toEqual([]);
    expect(errors).toEqual([]);
});

test('failed sign-out remains visibly pending across reload and retries the original session before restoring', async ({ page }) => {
    const user = userFixture({ isEmailConfirmed: false });
    await installClientAuthApiAsync(page, user);
    await page.route('**/api/v1/logs', async route => route.fulfill({ status: 204 }));
    await seedClientSessionAsync(page, user);
    let unavailable = true;
    const selectors: Array<string | undefined> = [];
    await page.route('**/api/v1/auth/logout', async route => {
        selectors.push(route.request().headers()['x-fooddiary-session']);
        if (unavailable) {
            await route.fulfill({ status: 503, json: {} });
        } else {
            await route.fallback();
        }
    });
    await page.goto('/verify-pending');
    await expect(page.getByRole('heading', { name: 'Check your email', exact: true })).toBeVisible();
    await page.getByRole('button', { name: 'Sign out and use another email', exact: true }).click();
    await expect(page.locator('fd-auth-recovery-notice')).toContainText('server has not confirmed sign-out');
    await page.reload();
    await expect(page.locator('fd-auth-recovery-notice')).toContainText('server has not confirmed sign-out');
    await expect(page.locator('fd-email-verification-pending')).toHaveCount(0);
    await page.getByRole('dialog', { name: 'Account access' }).getByRole('button', { name: 'Close', exact: true }).click();
    const pending: unknown = JSON.parse((await page.evaluate(() => localStorage.getItem('pendingBrowserLogout'))) ?? 'null');
    expect(pending).toMatchObject({
        sessionId: '00000000-0000-4000-8000-000000000002',
        contextId: null,
        telegram: false,
    });
    await page.screenshot({ path: test.info().outputPath('pending-logout-reload.png'), fullPage: true });
    unavailable = false;
    await page.locator('fd-auth-recovery-notice').getByRole('button', { name: 'Try again', exact: true }).click();
    await expect(page.locator('fd-auth-recovery-notice')).toBeEmpty();
    expect(await page.evaluate(() => localStorage.getItem('pendingBrowserLogout'))).toBeNull();
    expect(selectors.length).toBeGreaterThanOrEqual(2);
    expect(selectors.every(value => value === '00000000-0000-4000-8000-000000000002')).toBe(true);
});

test('signing in to a replacement account in another tab clears the old pending logout without revoking the replacement', async ({
    page,
    context,
}) => {
    const oldUser = userFixture({ isEmailConfirmed: false });
    const replacement = userFixture({ id: '00000000-0000-4000-8000-000000000003', username: 'replacement-user' });
    const other = await context.newPage();
    const firstState = await installClientAuthApiAsync(page, oldUser);
    const secondState = await installClientAuthApiAsync(other, oldUser);
    firstState.authenticated = true;
    secondState.authenticated = true;
    const logoutSelectors: Array<string | undefined> = [];
    let replaced = false;
    for (const tab of [page, other]) {
        await tab.route('**/api/v1/logs', async route => route.fulfill({ status: 204 }));
        await tab.route('**/api/v1/auth/session', async route => {
            await route.fulfill(
                jsonFixture(contract, 'POST', route.request().url(), {
                    sessionId: replaced ? '00000000-0000-4000-8000-000000000004' : '00000000-0000-4000-8000-000000000002',
                    roles: ['User'],
                    user: replaced ? replacement : oldUser,
                }),
            );
        });
        await tab.route('**/api/v1/auth/logout', async route => {
            logoutSelectors.push(route.request().headers()['x-fooddiary-session']);
            await route.fulfill({ status: 503, json: {} });
        });
    }
    await page.goto('/verify-pending');
    await expect(page.getByRole('heading', { name: 'Check your email', exact: true })).toBeVisible();
    await page.getByRole('button', { name: 'Sign out and use another email', exact: true }).click();
    await expect(page.locator('fd-auth-recovery-notice')).toContainText('server has not confirmed sign-out');
    await other.route('**/api/v1/auth/login', async route => {
        replaced = true;
        firstState.user = replacement;
        secondState.user = replacement;
        await route.fulfill(
            jsonFixture(contract, 'POST', route.request().url(), {
                sessionId: '00000000-0000-4000-8000-000000000004',
                roles: ['User'],
                user: replacement,
            }),
        );
    });
    await other.goto('/?auth=login');
    await other.getByLabel('Email', { exact: true }).fill('replacement@example.test');
    await other.getByLabel('Password', { exact: true }).fill('SyntheticPassword123!');
    await other.getByRole('button', { name: 'Login', exact: true }).click();
    await expect(other.locator('fd-dashboard')).toBeVisible();
    expect(await other.evaluate(() => localStorage.getItem('pendingBrowserLogout'))).toBeNull();
    await page.goto('/dashboard');
    await expect(page.locator('fd-dashboard')).toBeVisible();
    expect(await page.evaluate(() => sessionStorage.getItem('userId'))).toBe(replacement.id);
    expect(logoutSelectors.length).toBeGreaterThan(0);
    expect(logoutSelectors.every(value => value === '00000000-0000-4000-8000-000000000002')).toBe(true);
    await page.screenshot({ path: test.info().outputPath('replacement-account-first-tab.png'), fullPage: true });
});
