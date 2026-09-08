import { expect, type Page, test } from '@playwright/test';

import type { AdminDashboardOverview } from '../../projects/fooddiary-admin/src/app/features/admin-dashboard/models/admin-dashboard-overview.data';

const DAY_MS = 86_400_000;

test.describe('admin smoke', () => {
    test('redirects unauthenticated user to unauthorized page', async ({ page }) => {
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
    await page.addInitScript(
        (token: string) => {
            window.localStorage.setItem('authToken', token);
        },
        createJwt({ role: 'Admin' }),
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
        await route.fulfill(jsonResponse({ totalTokens: 12345, inputTokens: 7000, outputTokens: 5345 }));
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
