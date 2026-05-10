import { expect, type Page, test } from '@playwright/test';

test.describe('admin smoke', () => {
    test('redirects unauthenticated user to unauthorized page', async ({ page }) => {
        await page.goto('/users');

        await expect(page).toHaveURL(/\/unauthorized\?reason=unauthenticated/);
        await expect(page.getByRole('heading', { name: 'Access denied' })).toBeVisible();
        await expect(page.getByText('Your admin session is missing or expired.')).toBeVisible();
    });

    test('renders admin pages for authenticated admin with mocked api', async ({ page }) => {
        await authenticateAdminAsync(page);
        await mockAdminApiAsync(page);

        await page.goto('/');

        await expect(page.getByText('Total users')).toBeVisible();
        await expect(page.getByText('AI total tokens')).toBeVisible();

        await page.getByRole('link', { name: 'Users' }).click();
        await expect(page).toHaveURL(/\/users$/);
        await expect(page.getByPlaceholder('Search by email or username')).toBeVisible();
        await expect(page.getByText('Total users: 1')).toBeVisible();

        await page.getByRole('link', { name: 'AI Logs' }).click();
        await expect(page).toHaveURL(/\/ai-usage$/);
        await expect(page.getByText('Total tokens')).toBeVisible();
        await expect(page.getByText('12345')).toBeVisible();

        await page.getByRole('link', { name: 'Email templates' }).click();
        await expect(page).toHaveURL(/\/email-templates$/);
        await expect(page.getByRole('button', { name: 'Create template' })).toBeVisible();
        await expect(page.getByText('Verify your email')).toBeVisible();
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
    await page.route('**/api/v1/admin/dashboard', async route => {
        await route.fulfill(jsonResponse({ totalUsers: 42, activeUsers: 30, premiumUsers: 10 }));
    });

    await page.route('**/api/v1/admin/ai-usage/summary', async route => {
        await route.fulfill(jsonResponse({ totalTokens: 12345, inputTokens: 7000, outputTokens: 5345 }));
    });

    await page.route('**/api/v1/admin/users**', async route => {
        await route.fulfill(jsonResponse(createUsersPage()));
    });

    await page.route('**/api/v1/admin/email-templates**', async route => {
        await route.fulfill(jsonResponse([createEmailTemplate()]));
    });
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
