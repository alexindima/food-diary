import { expect, test } from '@playwright/test';

import { userFixture } from '../../src/testing/api-fixtures';
import { installClientAuthApiAsync, installVerificationHubAsync, seedClientSessionAsync } from '../support/auth-flow-fixtures';
import { jsonFixture, OpenApiFixtures } from '../support/openapi-fixtures.mjs';

const contract = new OpenApiFixtures();
const TICKET = 'abcdefghijklmnopqrstuvwxyz0123456789ABCDEFG';
const CALLBACK_CODE = 'synthetic-telegram-code';
const CALLBACK_STATE = 'synthetic-telegram-state';
const RECONNECT_OBSERVATION_MS = 3000;

test('Telegram callback exchanges once, removes credentials from the URL and preserves the signed-in session on reload', async ({
    page,
}) => {
    const state = await installClientAuthApiAsync(page);
    const runtimeErrors: string[] = [];
    page.on('pageerror', error => runtimeErrors.push(error.message));
    let exchanges = 0;
    let completions = 0;
    await page.route('**/api/v1/auth/telegram/configuration', async route => {
        await route.fulfill(
            jsonFixture(contract, 'GET', route.request().url(), { loginEnabled: true, registrationEnabled: true, oidcEnabled: true }),
        );
    });
    await page.route('**/api/v1/auth/telegram/oidc/exchange', async route => {
        exchanges++;
        expect(route.request().postDataJSON()).toEqual({ code: CALLBACK_CODE, state: CALLBACK_STATE });
        await route.fulfill(
            jsonFixture(contract, 'POST', route.request().url(), {
                ticket: TICKET,
                nextAction: 'login',
                expiresAtUtc: '2099-01-01T00:00:00Z',
            }),
        );
    });
    await page.route('**/api/v1/auth/telegram/complete', async route => {
        completions++;
        state.authenticated = true;
        expect(route.request().postDataJSON()).toMatchObject({ ticket: TICKET, action: 'login', language: 'en' });
        await route.fulfill(
            jsonFixture(contract, 'POST', route.request().url(), {
                sessionId: '00000000-0000-4000-8000-000000000002',
                roles: ['User'],
                user: state.user,
            }),
        );
    });

    await page.goto(`/auth/telegram?code=${CALLBACK_CODE}&state=${CALLBACK_STATE}`);
    await expect(page).toHaveURL(/\/auth\/telegram$/u);
    await page.locator('fd-telegram-auth').getByRole('button', { name: 'Open my diary', exact: true }).click();
    await expect(page).toHaveURL(/\/dashboard$/u);
    await expect(page.locator('fd-dashboard')).toBeVisible();
    await page.reload();
    await expect(page).toHaveURL(/\/dashboard$/u);
    await expect(page.locator('fd-dashboard')).toBeVisible();

    expect(exchanges).toBe(1);
    expect(completions).toBe(1);
    expect(state.unexpectedRequests).toEqual([]);
    expect(runtimeErrors).toEqual([]);
});

test('a real SignalR handshake and EmailVerified message unlock the account and close the page connection', async ({ page }) => {
    const state = await installClientAuthApiAsync(page, userFixture({ isEmailConfirmed: false }));
    await seedClientSessionAsync(page, state.user);
    const hub = await installVerificationHubAsync(page);
    const runtimeErrors: string[] = [];
    page.on('pageerror', error => runtimeErrors.push(error.message));

    await page.clock.install();
    await page.goto('/verify-pending');
    await expect(page.getByRole('heading', { name: 'Check your email', exact: true })).toBeVisible();
    await expect(page.locator('fd-email-verification-pending [role="status"]')).toHaveText('Waiting for verification...');
    await expect.poll(() => hub.handshakes).toBe(1);
    expect(await page.evaluate(() => sessionStorage.getItem('emailConfirmed'))).toBe('false');

    hub.verify();

    await expect(page).toHaveURL(/\/dashboard$/u);
    state.user = userFixture({ isEmailConfirmed: true });
    await expect(page.locator('fd-dashboard')).toBeVisible();
    await expect.poll(() => hub.closes).toBe(1);
    await page.clock.runFor(RECONNECT_OBSERVATION_MS);
    expect(hub.connections).toBe(1);
    expect(hub.handshakes).toBe(1);
    expect(hub.closes).toBe(1);
    expect(await page.evaluate(() => sessionStorage.getItem('emailConfirmed'))).toBe('true');
    expect(state.unexpectedRequests).toEqual([]);
    expect(runtimeErrors).toEqual([]);
});

test('signing out leaves the pending page through the SPA and closes its connection without confirming email', async ({ page }) => {
    const state = await installClientAuthApiAsync(page, userFixture({ isEmailConfirmed: false }));
    await seedClientSessionAsync(page, state.user);
    const hub = await installVerificationHubAsync(page);
    const runtimeErrors: string[] = [];
    page.on('pageerror', error => runtimeErrors.push(error.message));
    await page.clock.install();
    await page.goto('/verify-pending');
    await expect.poll(() => hub.handshakes).toBe(1);
    await page.locator('html').evaluate(element => {
        element.setAttribute('data-auth-flow-document', 'same-document');
    });

    await page.getByRole('button', { name: 'Sign out and use another email', exact: true }).click();

    await expect(page).toHaveURL(/\/\?auth=login$/u);
    await expect(page.locator('html')).toHaveAttribute('data-auth-flow-document', 'same-document');
    await expect.poll(() => hub.closes).toBe(1);
    await page.clock.runFor(RECONNECT_OBSERVATION_MS);
    expect(hub.connections).toBe(1);
    expect(hub.handshakes).toBe(1);
    expect(hub.closes).toBe(1);
    expect(await page.evaluate(() => sessionStorage.getItem('emailConfirmed'))).toBeNull();
    expect(await page.evaluate(() => sessionStorage.getItem('authToken'))).toBeNull();
    expect(state.unexpectedRequests).toEqual([]);
    expect(runtimeErrors).toEqual([]);
});
