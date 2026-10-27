import { expect, type Page, test } from '@playwright/test';

import { userFixture } from '../../src/testing/api-fixtures';
import { installClientAuthApiAsync } from '../support/auth-flow-fixtures';
import { jsonFixture, OpenApiFixtures } from '../support/openapi-fixtures.mjs';

const contract = new OpenApiFixtures();
const PROOF = 'synthetic-signed-telegram-launch';
const ACCESS = 'synthetic-mini-app-access';
const TICKET = 'abcdefghijklmnopqrstuvwxyz0123456789ABCDEFG';
const HTTP_BAD_REQUEST = 400;
const HTTP_UNAUTHORIZED = 401;
type MiniAppCalls = { begin: number; complete: number; refresh: number; sdk: number; logout: number; proof: string };
type MiniAppHarness = { state: Awaited<ReturnType<typeof installClientAuthApiAsync>>; calls: MiniAppCalls; errors: string[] };

async function installTelegramSdkAsync(page: Page, calls: MiniAppCalls, fails: boolean): Promise<void> {
    await page.route('https://telegram.org/js/telegram-web-app.js*', async route => {
        calls.sdk++;
        if (fails) {
            await route.abort('failed');
            return;
        }
        await route.fulfill({
            contentType: 'application/javascript',
            body: `window.Telegram = { WebApp: { initData: ${JSON.stringify(calls.proof)} } };`,
        });
    });
}

async function installMiniAppAsync(
    page: Page,
    options: {
        nextAction?: 'login' | 'onboarding' | 'link';
        beginStatus?: number;
        completionStatus?: number;
        restored?: boolean;
        sdkFails?: boolean;
        loginEnabled?: boolean;
        requiresVerification?: boolean;
    } = {},
): Promise<MiniAppHarness> {
    const user =
        options.requiresVerification === true
            ? userFixture({ isEmailConfirmed: false })
            : userFixture({ email: null, hasPassword: false, isEmailConfirmed: false });
    const state = await installClientAuthApiAsync(page, user);
    state.authenticated = options.restored ?? false;
    const calls = { begin: 0, complete: 0, refresh: 0, sdk: 0, logout: 0, proof: PROOF };
    const errors: string[] = [];
    page.on('pageerror', error => errors.push(error.message));
    await page.route('**/api/v1/logs', async route => route.fulfill({ status: 204 }));
    await installTelegramSdkAsync(page, calls, options.sdkFails === true);
    await page.route('**/api/v1/auth/refresh', async route => {
        calls.refresh++;
        expect(route.request().headers()['x-fooddiary-client']).toBe('telegram');
        if (!state.authenticated) {
            await route.fulfill({ status: 401, json: {} });
            return;
        }
        await route.fulfill(
            jsonFixture(contract, 'POST', route.request().url(), { user: state.user, roles: ['User'], accessToken: ACCESS }),
        );
    });
    await page.route('**/api/v1/auth/telegram/configuration', async route => {
        await route.fulfill(
            jsonFixture(contract, 'GET', route.request().url(), {
                loginEnabled: options.loginEnabled ?? true,
                registrationEnabled: true,
                oidcEnabled: true,
            }),
        );
    });
    await page.route('**/api/v1/auth/telegram/mini-app/begin', async route => {
        calls.begin++;
        expect(route.request().postDataJSON()).toEqual({ initData: calls.proof });
        expect(route.request().headers()['x-fooddiary-client']).toBe('telegram');
        if (options.beginStatus !== undefined) {
            await route.fulfill({ status: options.beginStatus, json: { error: 'Telegram.InvalidProof' } });
            return;
        }
        await route.fulfill(
            jsonFixture(contract, 'POST', route.request().url(), {
                ticket: TICKET,
                nextAction: options.nextAction ?? 'login',
                expiresAtUtc: '2099-01-01T00:00:00Z',
            }),
        );
    });
    await page.route('**/api/v1/auth/telegram/complete', async route => {
        calls.complete++;
        expect(route.request().postDataJSON()).toMatchObject({ ticket: TICKET, action: 'login' });
        expect(route.request().headers()['x-fooddiary-client']).toBe('telegram');
        if (options.completionStatus !== undefined) {
            await route.fulfill({ status: options.completionStatus, json: { error: 'Telegram.InvalidProof' } });
            return;
        }
        state.authenticated = true;
        await route.fulfill(
            jsonFixture(contract, 'POST', route.request().url(), { user: state.user, roles: ['User'], accessToken: ACCESS }),
        );
    });
    await page.route('**/api/v1/auth/logout', async route => {
        calls.logout++;
        state.authenticated = false;
        await route.fulfill({ status: 204 });
    });
    return { state, calls, errors };
}

test('Mini App opens a linked Telegram-only diary without credentials and restores Telegram transport on reload', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    const { state, calls, errors } = await installMiniAppAsync(page);
    await page.goto(`/#tgWebAppData=${PROOF}&tgWebAppVersion=8.0`);
    await expect(page).toHaveURL(/\/dashboard$/u);
    await expect(page.locator('fd-dashboard')).toBeVisible();
    expect(calls.begin).toBe(1);
    expect(calls.complete).toBe(1);
    expect(calls.sdk).toBe(1);
    await expect(page.locator('input[type="password"]')).toHaveCount(0);
    for (const key of ['authToken', 'refreshToken']) {
        expect(await page.evaluate(storageKey => localStorage.getItem(storageKey) ?? sessionStorage.getItem(storageKey), key)).toBeNull();
    }
    await page.screenshot({ path: test.info().outputPath('mini-app-linked-diary-mobile.png'), fullPage: true });
    const metadata = page.locator('meta[name="fooddiary-webmcp"]');
    const webmcp = (await metadata.count()) === 0 ? null : await metadata.getAttribute('content');
    await test.info().attach('webmcp-runtime-status', {
        body: webmcp ?? 'No WebMCP metadata in this controlled build',
        contentType: 'application/json',
    });
    await page.reload();
    await expect(page.locator('fd-dashboard')).toBeVisible();
    expect(calls.refresh).toBe(2);
    expect(calls.begin).toBe(1);
    expect(calls.complete).toBe(1);
    expect(state.unexpectedRequests).toEqual([]);
    expect(errors).toEqual([]);
});

test('Mini App opens its requested protected screen after signing in', async ({ page }) => {
    const { calls, errors } = await installMiniAppAsync(page);
    await page.goto(`/profile#tgWebAppData=${PROOF}`);
    await expect(page).toHaveURL(/\/profile$/u);
    await expect(page.locator('input[type="password"]')).toHaveCount(0);
    expect(calls.complete).toBe(1);
    expect(errors).toEqual([]);
});

test('Mini App onboarding stays explicit and its checked choice survives reload', async ({ page }) => {
    const { calls } = await installMiniAppAsync(page, { nextAction: 'onboarding' });
    await page.goto(`/#tgWebAppData=${PROOF}`);
    await expect(page.getByRole('button', { name: 'Create a new diary', exact: true })).toBeVisible();
    expect(calls.complete).toBe(0);
    await page.reload();
    await expect(page.getByRole('button', { name: 'Create a new diary', exact: true })).toBeVisible();
    expect(calls.begin).toBe(1);
    expect(calls.complete).toBe(0);
    await expect(page.locator('input[type="password"]')).toHaveCount(0);
});

for (const status of [HTTP_BAD_REQUEST, HTTP_UNAUTHORIZED]) {
    test(`Mini App rejects an invalid or expired proof (${status}) without falling back to password login`, async ({ page }) => {
        const { calls } = await installMiniAppAsync(page, { beginStatus: status });
        await page.goto(`/#tgWebAppData=${PROOF}`);
        await expect(page.locator('fd-telegram-auth [role="alert"]')).toBeVisible();
        await expect(page.locator('input[type="password"]')).toHaveCount(0);
        expect(calls.begin).toBe(1);
        expect(calls.complete).toBe(0);
    });
}

test('Mini App completion failure keeps the user on the Telegram entry screen', async ({ page }) => {
    const { calls } = await installMiniAppAsync(page, { completionStatus: 400 });
    await page.goto(`/#tgWebAppData=${PROOF}`);
    await expect(page.locator('fd-telegram-auth [role="alert"]')).toBeVisible();
    expect(calls.complete).toBe(1);
    await expect(page.locator('fd-dashboard')).toHaveCount(0);
});

test('Mini App SDK failure exposes an error without sending unverified identity', async ({ page }) => {
    const { calls } = await installMiniAppAsync(page, { sdkFails: true });
    await page.goto(`/#tgWebAppData=${PROOF}`);
    await expect(page.locator('fd-telegram-auth [role="alert"]')).toBeVisible();
    expect(calls.begin).toBe(0);
    expect(calls.complete).toBe(0);
});

test('Mini App preserves a restored session without consuming the launch proof again', async ({ page }) => {
    const { calls } = await installMiniAppAsync(page, { restored: true });
    await page.goto(`/#tgWebAppData=${PROOF}`);
    await expect(page.locator('fd-dashboard')).toBeVisible();
    expect(calls.begin).toBe(0);
    expect(calls.complete).toBe(0);
    expect(calls.sdk).toBe(0);
});

test('Mini App respects explicit logout across reload and allows a genuinely new launch', async ({ page }) => {
    const { calls } = await installMiniAppAsync(page, { requiresVerification: true });
    await page.goto(`/#tgWebAppData=${PROOF}`);
    await expect(page).toHaveURL(/\/verify-pending$/u);
    await page.getByRole('button', { name: 'Sign out and use another email', exact: true }).click();
    await expect(page).toHaveURL(/\/\?auth=login$/u);
    expect(calls.logout).toBe(1);
    await page.reload();
    await expect(page).toHaveURL(/\/\?auth=login$/u);
    await expect(page.locator('input[type="password"]')).toBeVisible();
    expect(calls.begin).toBe(1);
    calls.proof = 'new-synthetic-telegram-launch';
    await page.goto(`/#tgWebAppData=${calls.proof}`);
    await expect(page).toHaveURL(/\/verify-pending$/u);
    expect(calls.begin).toBe(2);
});

test('disabled Mini App login preserves the feature switch', async ({ page }) => {
    const { calls } = await installMiniAppAsync(page, { loginEnabled: false });
    await page.goto(`/#tgWebAppData=${PROOF}`);
    await expect(page.getByText('Telegram sign-in is currently unavailable.', { exact: false })).toBeVisible();
    expect(calls.begin).toBe(0);
    expect(calls.complete).toBe(0);
});

test('ordinary website visits do not start Telegram authorization', async ({ page }) => {
    const { calls } = await installMiniAppAsync(page);
    await page.goto('/');
    await expect(page.locator('fd-main')).toBeVisible();
    expect(calls.begin).toBe(0);
    expect(calls.complete).toBe(0);
    expect(calls.sdk).toBe(0);
});
