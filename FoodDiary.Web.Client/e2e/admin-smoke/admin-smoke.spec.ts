import { expect, type Page, test } from '@playwright/test';

import type { AdminAiUsageSummary } from '../../projects/fooddiary-admin/src/app/features/admin-ai-usage/models/admin-ai-usage.data';
import type { AdminDashboardOverview } from '../../projects/fooddiary-admin/src/app/features/admin-dashboard/models/admin-dashboard-overview.data';
import { inspectPage } from '../../quality/modern-web-guidance/browser.mjs';

const DAY_MS = 86_400_000;
const HTTP_UNAUTHORIZED = 401;
const GUIDANCE_MOBILE_WIDTH = 390;
const GUIDANCE_DESKTOP_WIDTH = 1440;
const GUIDANCE_TIMEOUT_MS = 120_000;

test.describe('admin guidance accessibility', () => {
    for (const width of [GUIDANCE_MOBILE_WIDTH, GUIDANCE_DESKTOP_WIDTH]) {
        test(`keeps admin content accessible with visible keyboard focus at ${width}px`, async ({ page }, testInfo) => {
            test.setTimeout(GUIDANCE_TIMEOUT_MS);
            await page.setViewportSize({ width, height: 900 });
            await page.emulateMedia({ forcedColors: width === GUIDANCE_DESKTOP_WIDTH ? 'active' : 'none' });
            await authenticateAdminAsync(page);
            await mockAdminApiAsync(page);
            for (const path of ['/', '/users', '/ai-usage', '/email-templates']) {
                await page.goto(path);
                await page.locator('html[data-app-ready] main h1').waitFor();
                for (const check of [
                    'browser:axe',
                    'browser:skip-link',
                    'browser:page-structure',
                    'browser:narrow-reflow',
                    'browser:focus-visible',
                ]) {
                    const result = await inspectPage(page, check, { kind: 'public', locale: 'en' });
                    await testInfo.attach(`${path === '/' ? 'dashboard' : path.replaceAll('/', '-')}-${check.slice('browser:'.length)}`, {
                        body: JSON.stringify(result, null, 2),
                        contentType: 'application/json',
                    });
                    expect(result.observations, `${path}: ${check} executes assertions`).toBeGreaterThan(0);
                    expect(result.findings, `${path}: ${check}`).toEqual([]);
                }
            }
        });
    }
});

test.describe('admin smoke', () => {
    for (const session of ['empty', 'expired'] as const) {
        test(`opens from dashboard SSO on the first attempt with an ${session} session`, async ({ page }) => {
            const freshSession = '00000000-0000-4000-8000-000000000002';
            if (session === 'expired') {
                await page.addInitScript(
                    token => {
                        localStorage.setItem('authToken', token);
                    },
                    createJwt({ role: 'Admin', exp: 1 }),
                );
            }
            await mockAdminApiAsync(page);
            let exchanges = 0;
            let dashboardRequests = 0;
            page.on('request', request => {
                if (request.url().includes('/admin/dashboard/overview')) {
                    dashboardRequests++;
                    expect(request.headers()['authorization']).toBeUndefined();
                    expect(request.headers()['x-fooddiary-session']).toBe(freshSession);
                }
            });
            await page.route('**/api/v1/auth/admin-sso/exchange', async route => {
                exchanges++;
                expect(route.request().postDataJSON()).toEqual({ code: 'dashboard-code' });
                expect(dashboardRequests).toBe(0);
                await route.fulfill(jsonResponse({ sessionId: freshSession, roles: ['Admin'] }));
            });

            await page.goto('/#code=dashboard-code');

            await expect(page.getByText('Total accounts', { exact: true })).toBeVisible();
            await expect(page.getByRole('heading', { name: 'Access denied' })).toHaveCount(0);
            expect(exchanges).toBe(1);
            await page.getByRole('link', { name: 'Accounts', exact: true }).click();
            await expect(page.getByText('Total users: 1')).toBeVisible();
            expect(exchanges).toBe(1);
        });
    }

    test('redirects unauthenticated user to unauthorized page', async ({ page }) => {
        await page.route('**/api/v1/auth/session', async route => route.fulfill({ status: HTTP_UNAUTHORIZED, json: {} }));
        await page.goto('/users');

        await expect(page).toHaveURL(/\/unauthorized\?reason=unauthenticated/);
        await expect(page.getByRole('heading', { name: 'Access denied' })).toBeVisible();
        await expect(page.getByText('Your admin session is missing or expired.')).toBeVisible();
    });

    test('renders admin pages for authenticated admin with mocked api', async ({ page }) => {
        const consoleErrors: string[] = [];
        page.on('pageerror', error => consoleErrors.push(error.message));
        page.on('console', message => {
            if (message.type() === 'error') {
                consoleErrors.push(message.text());
            }
        });
        await authenticateAdminAsync(page);
        await mockAdminApiAsync(page);

        await page.goto('/');

        await expect(page.getByText('Total accounts', { exact: true })).toBeVisible();
        await expect(page.getByText('42', { exact: true })).toBeVisible();
        await expect(page.getByRole('heading', { name: 'AI tokens', exact: true })).toBeVisible();
        await expect(page.getByText('Successful payments', { exact: true })).toBeVisible();

        const refreshed = page.waitForResponse(response => response.url().includes('/admin/dashboard/overview?'));
        await page.getByRole('button', { name: 'Refresh', exact: true }).click();
        const response = await refreshed;
        expect(response.ok()).toBe(true);
        await expect(page.getByText('Total accounts', { exact: true })).toBeVisible();

        await page.getByRole('link', { name: 'Accounts', exact: true }).click();
        await expect(page).toHaveURL(/\/users$/);
        await expect(page.getByRole('textbox', { name: 'Search users' })).toBeVisible();
        await expect(page.getByText('Total users: 1')).toBeVisible();

        await page.getByRole('link', { name: 'AI usage', exact: true }).click();
        await expect(page).toHaveURL(/\/ai-usage$/);
        await expect(page.locator('span').filter({ hasText: /^Total tokens$/ })).toBeVisible();
        await expect(page.getByText('12,345', { exact: true })).toBeVisible();

        await page.getByRole('link', { name: 'Email templates' }).click();
        await expect(page).toHaveURL(/\/email-templates$/);
        await expect(page.getByRole('button', { name: 'Create template' })).toBeVisible();
        await expect(page.getByText('Verify your email')).toBeVisible();
        expect(consoleErrors).toEqual([]);
    });
});

async function authenticateAdminAsync(page: Page): Promise<void> {
    await page.route('**/api/v1/auth/session', async route =>
        route.fulfill(jsonResponse({ sessionId: '00000000-0000-4000-8000-000000000002', roles: ['Admin'] })),
    );
}

async function mockAdminApiAsync(page: Page): Promise<void> {
    await page.route('**/api/v1/admin/dashboard/overview?*', async route => {
        const params = new URL(route.request().url()).searchParams;
        const from = params.get('from') ?? '';
        const to = params.get('to') ?? '';
        expect(from).toMatch(/^\d{4}-\d{2}-\d{2}$/);
        expect(to).toMatch(/^\d{4}-\d{2}-\d{2}$/);
        await route.fulfill(jsonResponse(createDashboardOverview(from, to)));
    });

    await page.route('**/api/v1/admin/ai-usage/summary**', async route => {
        const summary: AdminAiUsageSummary = {
            totalTokens: 12345,
            inputTokens: 7000,
            outputTokens: 5345,
            byDay: [],
            byOperation: [],
            byModel: [],
            byUser: [],
        };
        await route.fulfill(jsonResponse(summary));
    });

    await page.route('**/api/v1/admin/users**', async route => {
        if (new URL(route.request().url()).pathname.endsWith('/login-summary')) {
            await route.fulfill(jsonResponse([{ key: 'device:Desktop', count: 12 }]));
            return;
        }

        await route.fulfill(jsonResponse(createUsersPage()));
    });

    await page.route('**/api/v1/admin/email-templates**', async route => {
        await route.fulfill(jsonResponse([createEmailTemplate()]));
    });
}

function createDashboardOverview(from: string, to: string): AdminDashboardOverview {
    const fromUtc = `${from}T00:00:00.000Z`;
    const toUtc = new Date(Date.parse(`${to}T00:00:00Z`) + DAY_MS).toISOString();
    return {
        fromUtc,
        toUtc,
        interval: 'day',
        period: {
            fromUtc,
            toUtc,
            metrics: {
                registrations: 5,
                payingUsers: 2,
                aiTokens: 12345,
                trend: [{ date: fromUtc, registrations: 5, aiTokens: 12345, revenue: [{ currency: 'USD', gross: 25 }] }],
            },
            currencies: [{ currency: 'USD', gross: 25, net: 25, refunds: 0, chargebacks: 0, successfulPayments: 2 }],
        },
        previous: null,
        totalUsersNow: 42,
        premiumUsersNow: 10,
        pendingReportsNow: 0,
    };
}

function createUsersPage(): Record<string, unknown> {
    return {
        data: [
            {
                id: 'u1',
                email: 'admin@example.com',
                username: 'alex',
                isActive: true,
                isEmailConfirmed: true,
                createdOnUtc: '2026-01-01T00:00:00Z',
                roles: ['Admin'],
                deletedAt: null,
            },
        ],
        page: 1,
        limit: 20,
        totalPages: 1,
        totalItems: 1,
    };
}

function createEmailTemplate(): Record<string, unknown> {
    return {
        id: 't1',
        key: 'email_verification',
        locale: 'en',
        subject: 'Verify your email',
        htmlBody: '<p>Hello</p>',
        textBody: 'Hello',
        isActive: true,
        createdOnUtc: '2026-01-01T00:00:00Z',
        updatedOnUtc: null,
    };
}

function jsonResponse(body: unknown): { status: number; contentType: string; body: string } {
    return {
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(body),
    };
}

function createJwt(payload: Record<string, unknown>): string {
    return `${encodeSegment({ alg: 'none', typ: 'JWT' })}.${encodeSegment(payload)}.signature`;
}

function encodeSegment(value: Record<string, unknown>): string {
    return Buffer.from(JSON.stringify(value), 'utf8').toString('base64url');
}
