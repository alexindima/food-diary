import { expect, type Page, type Route, test } from '@playwright/test';

const MS_PER_SECOND = 1000;
const AUTH_TOKEN_TTL_SECONDS = 3600;
const CLIENT_API_MOCKS: readonly ClientApiMock[] = [
    { matches: pathname => pathname.endsWith('/users/info'), createResponse: createUser },
    { matches: pathname => pathname.endsWith('/dashboard'), createResponse: createDashboardSnapshot },
    { matches: pathname => pathname.endsWith('/cycles/current'), createResponse: () => null },
    { matches: pathname => pathname.endsWith('/tdee/insight'), createResponse: createTdeeInsight },
    { matches: pathname => pathname.endsWith('/usda/daily-micronutrients'), createResponse: createDailyMicronutrients },
    { matches: pathname => pathname.endsWith('/notifications/unread-count'), createResponse: () => ({ count: 2 }) },
    { matches: pathname => pathname.endsWith('/notifications'), createResponse: () => [] },
    { matches: pathname => pathname.endsWith('/favorite-products'), createResponse: createEmptyProductsPage },
    { matches: pathname => pathname.endsWith('/products/search'), createResponse: createProductsPage },
    { matches: pathname => pathname.endsWith('/products'), createResponse: createProductsPage },
];

test.describe('client smoke', () => {
    test('renders public landing page', async ({ page }) => {
        await page.goto('/');

        await expect(page.locator('fd-hero')).toBeVisible();
        await expect(page.locator('fd-features')).toBeVisible();
    });

    test('opens auth dialog on auth route', async ({ page }) => {
        await page.goto('/auth/login');

        await expect(page.locator('fd-auth-dialog fd-auth')).toBeVisible();
        await expect(page.locator('fd-auth .auth__form')).toBeVisible();
    });

    test('redirects authenticated user from landing to dashboard', async ({ page }) => {
        await page.addInitScript((token: string) => {
            window.localStorage.setItem('authToken', token);
            window.localStorage.setItem('refreshToken', 'refresh-token');
            window.localStorage.setItem('userId', 'u1');
            window.localStorage.setItem('emailConfirmed', 'true');
        }, createAuthenticatedUserJwt());

        await mockAuthenticatedClientApiAsync(page);

        await page.goto('/');

        await expect(page).toHaveURL(/\/dashboard$/);
        await expect(page.locator('fd-dashboard')).toBeVisible();
        await expect(page.getByRole('heading', { name: 'Consumption for today' })).toBeVisible();
    });

    test('renders not found page for unknown route', async ({ page }) => {
        await page.goto('/missing-page');

        await expect(page).toHaveURL(/\/missing-page$/);
        await expect(page.getByRole('heading')).toContainText('Page Not Found');
    });

    test('renders products page for authenticated user on mobile viewport', async ({ page }) => {
        await page.setViewportSize({ width: 390, height: 844 });
        await page.addInitScript((token: string) => {
            window.localStorage.setItem('authToken', token);
            window.localStorage.setItem('refreshToken', 'refresh-token');
            window.localStorage.setItem('userId', 'u1');
            window.localStorage.setItem('emailConfirmed', 'true');
        }, createAuthenticatedUserJwt());

        await mockAuthenticatedClientApiAsync(page);

        await page.goto('/products');

        await expect(page).toHaveURL(/\/products$/);
        await expect(page.getByRole('heading', { name: 'Products' })).toBeVisible();
        await expect(page.locator('.product-list__mobile-toolbar')).toBeVisible();
        await expect(page.getByRole('button', { name: 'Create' })).toBeVisible();
    });
});

async function mockAuthenticatedClientApiAsync(page: Page): Promise<void> {
    await page.route('**/hubs/notifications/**', async route => {
        await route.abort('failed');
    });

    await page.route('**/api/v1/**', fulfillClientApiRouteAsync);
}

async function fulfillClientApiRouteAsync(route: Route): Promise<void> {
    const { pathname } = new URL(route.request().url());
    await route.fulfill(jsonResponse(resolveClientApiResponse(pathname)));
}

function resolveClientApiResponse(pathname: string): unknown {
    const mock = CLIENT_API_MOCKS.find(item => item.matches(pathname));
    return mock === undefined ? {} : mock.createResponse();
}

function createDashboardSnapshot(): Record<string, unknown> {
    return {
        date: '2026-04-19T00:00:00.000Z',
        dailyGoal: 1900,
        weeklyCalorieGoal: 13300,
        statistics: {
            totalCalories: 820,
            averageProteins: 60,
            averageFats: 28,
            averageCarbs: 82,
            averageFiber: 14,
            proteinGoal: 120,
            fatGoal: 60,
            carbGoal: 180,
            fiberGoal: 30,
        },
        weeklyCalories: [],
        weight: {
            latest: { date: '2026-04-18T00:00:00.000Z', weight: 72.4 },
            previous: { date: '2026-04-11T00:00:00.000Z', weight: 72.9 },
            desired: 68,
        },
        waist: {
            latest: { date: '2026-04-18T00:00:00.000Z', circumference: 81 },
            previous: { date: '2026-04-11T00:00:00.000Z', circumference: 82 },
            desired: 76,
        },
        meals: {
            items: [],
            total: 0,
        },
        hydration: {
            goalMl: 2200,
            totalMl: 800,
            entries: [],
        },
        advice: {
            tone: 'supportive',
            title: 'Keep going',
            summary: 'Good start for the day.',
            actionLabel: null,
            actionUrl: null,
        },
        currentFastingSession: null,
        weightTrend: [],
        waistTrend: [],
        dashboardLayout: null,
        caloriesBurned: 250,
    };
}

function createUser(): Record<string, unknown> {
    return {
        id: 'u1',
        email: 'user@example.com',
        username: 'alexi',
        language: 'en',
        theme: 'default',
        uiStyle: 'classic',
        pushNotificationsEnabled: true,
        fastingPushNotificationsEnabled: true,
        socialPushNotificationsEnabled: false,
        fastingCheckInReminderHours: 4,
        fastingCheckInFollowUpReminderHours: 2,
        dashboardLayout: null,
        isActive: true,
        isEmailConfirmed: true,
        aiConsentAcceptedAt: null,
    };
}

function createTdeeInsight(): Record<string, unknown> {
    return {
        estimatedTdee: 2100,
        adaptiveTdee: 2050,
        bmr: 1500,
        suggestedCalorieTarget: 1900,
        currentCalorieTarget: 1900,
        weightTrendPerWeek: -0.2,
        confidence: 'medium',
        dataDaysUsed: 14,
        goalAdjustmentHint: null,
    };
}

function createDailyMicronutrients(): Record<string, unknown> {
    return {
        date: '2026-04-19T00:00:00.000Z',
        linkedProductCount: 0,
        totalProductCount: 0,
        nutrients: [],
        healthScores: null,
    };
}

function createEmptyProductsPage(): Record<string, unknown> {
    return {
        data: [],
        page: 1,
        limit: 20,
        totalPages: 0,
        totalItems: 0,
    };
}

function createProductsPage(): Record<string, unknown> {
    return {
        data: [
            {
                id: 'p1',
                name: 'Greek yogurt',
                brand: 'Food Diary',
                caloriesPerBase: 95,
                imageUrl: null,
                imageAssetId: null,
            },
        ],
        page: 1,
        limit: 20,
        totalPages: 1,
        totalItems: 1,
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

function createAuthenticatedUserJwt(): string {
    return createJwt({
        sub: 'u1',
        nameid: 'u1',
        role: 'User',
        exp: Math.floor(Date.now() / MS_PER_SECOND) + AUTH_TOKEN_TTL_SECONDS,
    });
}

function encodeSegment(value: Record<string, unknown>): string {
    return Buffer.from(JSON.stringify(value), 'utf8').toString('base64url');
}

type ClientApiMock = {
    matches: (pathname: string) => boolean;
    createResponse: () => unknown;
};
