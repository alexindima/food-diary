import { expect, type Page, type Route, test } from '@playwright/test';

const MS_PER_SECOND = 1000;
const AUTH_TOKEN_TTL_SECONDS = 3600;
const SESSION_RESTORE_DELAY_MS = 750;
const TEST_IMAGE_URLS = [
    createSvgDataUrl('#f97316', '1'),
    createSvgDataUrl('#22c55e', '2'),
    createSvgDataUrl('#0ea5e9', '3'),
    createSvgDataUrl('#a855f7', '4'),
] as const;
const CLIENT_API_MOCKS: readonly ClientApiMock[] = [
    { matches: pathname => pathname.endsWith('/users/info'), createResponse: createUser },
    { matches: pathname => pathname.endsWith('/dashboard'), createResponse: createDashboardSnapshot },
    { matches: pathname => pathname.endsWith('/consumptions/overview'), createResponse: createMealsOverview },
    { matches: pathname => pathname.endsWith('/cycles/current'), createResponse: () => null },
    { matches: pathname => pathname.endsWith('/tdee/insight'), createResponse: createTdeeInsight },
    { matches: pathname => pathname.endsWith('/usda/daily-micronutrients'), createResponse: createDailyMicronutrients },
    { matches: pathname => pathname.endsWith('/notifications/unread-count'), createResponse: () => ({ count: 2 }) },
    { matches: pathname => pathname.endsWith('/notifications'), createResponse: () => [] },
    { matches: pathname => pathname.endsWith('/recommendations'), createResponse: createRecommendations },
    { matches: pathname => pathname.endsWith('/favorite-products'), createResponse: createEmptyProductsPage },
    { matches: pathname => pathname.endsWith('/products/overview'), createResponse: createProductsOverview },
    { matches: pathname => pathname.endsWith('/products/search'), createResponse: createProductsPage },
    { matches: pathname => pathname.endsWith('/products'), createResponse: createProductsPage },
];

test.describe('client smoke', () => {
    test('renders public landing page', async ({ page }) => {
        await page.goto('/');

        await expect(page.locator('fd-hero')).toBeVisible();
        await expect(page.locator('fd-features')).toBeVisible();
    });

    test('opens auth dialog from auth query param', async ({ page }) => {
        await page.goto('/?auth=login');

        await expect(page.locator('fd-auth-dialog fd-auth')).toBeVisible();
        await expect(page.locator('fd-auth .auth__form')).toBeVisible();
    });
});

test.describe('client auth smoke', () => {
    test('renders standalone login and registration on mobile', async ({ page }) => {
        await page.setViewportSize({ width: 390, height: 844 });
        await page.goto('/mobile/login');

        await expect(page.getByRole('heading', { name: 'Food Diary', level: 1 })).toBeVisible();
        await expect(page.getByRole('tab', { name: 'Login' })).toHaveAttribute('aria-selected', 'true');
        await page.getByRole('tab', { name: 'Register' }).click();
        await expect(page.getByRole('checkbox', { name: 'I agree to the Privacy Policy' })).toBeVisible();
        await expect(page.getByRole('button', { name: 'Register' })).toBeVisible();
        await expectNoHorizontalOverflowAsync(page);
    });

    test('routes an invalid email verification link back to login', async ({ page }) => {
        await page.goto('/verify-email');

        await expect(page.getByRole('heading', { name: 'Email verification', level: 1 })).toBeVisible();
        await expect(page.getByText('Verification link is invalid or expired.')).toBeVisible();
        await page.getByRole('button', { name: 'Back to login' }).click();

        await expect(page).toHaveURL(/\?auth=login$/);
        await expect(page.locator('fd-auth-dialog fd-auth')).toBeVisible();
    });

    test('keeps email verification request failures retryable', async ({ page }) => {
        let verificationAttempts = 0;
        await page.route('**/api/v1/auth/verify-email', async route => {
            verificationAttempts += 1;
            await route.fulfill({ status: 400, contentType: 'application/json', body: '{}' });
        });

        await page.goto('/verify-email?userId=user-1&token=invalid-token');
        await expect(page.getByText("We couldn't verify the email. Try again or request a new link.")).toBeVisible();
        await page.getByRole('button', { name: 'Try again' }).click();
        await expect(page.getByRole('button', { name: 'Try again' })).toBeVisible();

        expect(verificationAttempts).toBe(2);
    });

    test('keeps password reset form available after an API error', async ({ page }) => {
        await page.route('**/api/v1/auth/password-reset/confirm', async route => {
            await route.fulfill({ status: 400, contentType: 'application/json', body: '{}' });
        });

        await page.goto('/reset-password?userId=user-1&token=invalid-token');
        await page.getByLabel('New password', { exact: true }).fill('reviewPass123');
        await page.getByLabel('Confirm new password', { exact: true }).fill('reviewPass123');
        await page.getByRole('button', { name: 'Save new password' }).click();

        await expect(page.getByText("We couldn't reset the password. Please try again.")).toBeVisible();
        await expect(page.getByLabel('New password', { exact: true })).toHaveValue('reviewPass123');
        await expect(page.getByRole('button', { name: 'Save new password' })).toBeEnabled();
    });

    test('renders and updates email verification pending state on mobile', async ({ page }) => {
        await page.setViewportSize({ width: 390, height: 844 });
        await page.addInitScript((token: string) => {
            window.localStorage.setItem('authToken', token);
            window.localStorage.setItem('refreshToken', 'refresh-token');
            window.localStorage.setItem('userId', 'u1');
            window.localStorage.setItem('emailConfirmed', 'false');
        }, createAuthenticatedUserJwt());
        await page.route('**/hubs/**', async route => {
            await route.abort('failed');
        });
        await page.route('**/api/v1/users/info', async route => {
            await route.fulfill(jsonResponse({ ...createUser(), isEmailConfirmed: false }));
        });
        await page.route('**/api/v1/auth/verify-email/resend', async route => {
            await route.fulfill({ status: 204 });
        });

        await page.goto('/verify-pending');
        await expect(page.getByRole('heading', { name: 'Check your email', level: 1 })).toBeVisible();
        await page.getByRole('button', { name: "I've verified, refresh status" }).click();
        await expect(page.getByText('Still not confirmed. Please check your inbox or spam folder.')).toBeVisible();
        await page.getByRole('button', { name: 'Send again' }).click();
        await expect(page.getByText('Verification email sent.')).toBeVisible();
        await expect(page.getByRole('button', { name: 'Send again' })).toBeDisabled();
        await expectNoHorizontalOverflowAsync(page);
    });
});

test.describe('authenticated client smoke', () => {
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
});

test.describe('session routing smoke', () => {
    test('keeps prerendered landing hidden while an authenticated root route initializes', async ({ page }) => {
        await page.addInitScript((token: string) => {
            window.localStorage.setItem('authToken', token);
            window.localStorage.setItem('refreshToken', 'refresh-token');
            window.localStorage.setItem('userId', 'u1');
            window.localStorage.setItem('emailConfirmed', 'true');
        }, createAuthenticatedUserJwt());
        await mockAuthenticatedClientApiAsync(page);
        await page.route('**/api/v1/users/info', async route => {
            await new Promise(resolve => setTimeout(resolve, SESSION_RESTORE_DELAY_MS));
            await route.fulfill(jsonResponse(createUser()));
        });

        await page.goto('/', { waitUntil: 'domcontentloaded' });

        await expect(page.locator('html')).toHaveClass(/fd-session-route-pending/);
        await expect(page.locator('fd-hero')).toBeHidden();
        await expect(page).toHaveURL(/\/dashboard$/);
        await expect(page.locator('html')).not.toHaveClass(/fd-session-route-pending/);
    });
});

test.describe('authenticated feature smoke', () => {
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
        await page.getByRole('button', { name: 'More actions' }).click();
        await expect(page.getByRole('menuitem', { name: 'Create' })).toBeVisible();
        await expect(page.getByRole('menuitem', { name: 'Filters' })).toBeVisible();
    });

    test('renders recommendations and marks selected item as read', async ({ page }) => {
        await page.addInitScript((token: string) => {
            window.localStorage.setItem('authToken', token);
            window.localStorage.setItem('refreshToken', 'refresh-token');
            window.localStorage.setItem('userId', 'u1');
            window.localStorage.setItem('emailConfirmed', 'true');
        }, createAuthenticatedUserJwt());

        await mockAuthenticatedClientApiAsync(page);

        await page.goto('/recommendations?recommendationId=rec-1');

        await expect(page).toHaveURL(/\/recommendations\?recommendationId=rec-1$/);
        await expect(page.getByRole('heading', { name: 'Recommendations' })).toBeVisible();
        await expect(page.getByText('Add a protein source to breakfast.')).toBeVisible();
        await expect(page.getByText('From Ada Lovelace')).toBeVisible();
        await expect(page.getByRole('button', { name: /From Ada Lovelace.*Read/s })).toBeVisible();
    });

    test('keeps meal collage thumbnails inside the media slot', async ({ page }) => {
        await page.setViewportSize({ width: 1280, height: 900 });
        await page.addInitScript((token: string) => {
            window.localStorage.setItem('authToken', token);
            window.localStorage.setItem('refreshToken', 'refresh-token');
            window.localStorage.setItem('userId', 'u1');
            window.localStorage.setItem('emailConfirmed', 'true');
        }, createAuthenticatedUserJwt());

        await mockAuthenticatedClientApiAsync(page);

        await page.goto('/meals');

        await expect(page).toHaveURL(/\/meals$/);
        await expect(page.getByRole('heading', { name: 'Meals' })).toBeVisible();
        await expect(page.locator('.entity-card__collage')).toHaveCount(2);

        await expectCollageFitsMediaSlotAsync(page.locator('.entity-card__collage--count-4'));
        await expectCollageFitsMediaSlotAsync(page.locator('.entity-card__collage--count-2'));
    });
});

async function expectCollageFitsMediaSlotAsync(collage: ReturnType<Page['locator']>): Promise<void> {
    await expect(collage).toBeVisible();

    const dimensions = await collage.evaluate(element => {
        const media = element.closest('.media-card__media');
        const collageRect = element.getBoundingClientRect();
        const mediaRect = media?.getBoundingClientRect();
        const imageRects = Array.from(element.querySelectorAll('img')).map(image => {
            const rect = image.getBoundingClientRect();
            return {
                bottom: rect.bottom - collageRect.top,
                height: rect.height,
                left: rect.left - collageRect.left,
                right: rect.right - collageRect.left,
                top: rect.top - collageRect.top,
                width: rect.width,
            };
        });

        return {
            collageHeight: collageRect.height,
            collageWidth: collageRect.width,
            imageRects,
            mediaHeight: mediaRect?.height ?? 0,
            mediaWidth: mediaRect?.width ?? 0,
        };
    });

    expect(dimensions.collageWidth).toBeGreaterThan(0);
    expect(dimensions.collageHeight).toBeGreaterThan(0);
    expect(Math.abs(dimensions.collageWidth - dimensions.mediaWidth)).toBeLessThanOrEqual(1);
    expect(Math.abs(dimensions.collageHeight - dimensions.mediaHeight)).toBeLessThanOrEqual(1);
    expect(Math.abs(dimensions.collageWidth - dimensions.collageHeight)).toBeLessThanOrEqual(1);

    for (const imageRect of dimensions.imageRects) {
        expect(imageRect.width).toBeGreaterThan(0);
        expect(imageRect.height).toBeGreaterThan(0);
        expect(imageRect.left).toBeGreaterThanOrEqual(0);
        expect(imageRect.top).toBeGreaterThanOrEqual(0);
        expect(imageRect.right).toBeLessThanOrEqual(dimensions.collageWidth + 1);
        expect(imageRect.bottom).toBeLessThanOrEqual(dimensions.collageHeight + 1);
    }
}

async function expectNoHorizontalOverflowAsync(page: Page): Promise<void> {
    const dimensions = await page.evaluate(() => ({
        clientWidth: document.documentElement.clientWidth,
        scrollWidth: document.documentElement.scrollWidth,
    }));

    expect(dimensions.scrollWidth).toBe(dimensions.clientWidth);
}

async function mockAuthenticatedClientApiAsync(page: Page): Promise<void> {
    await page.route('**/hubs/notifications/**', async route => {
        await route.abort('failed');
    });

    await page.route('**/api/v1/**', fulfillClientApiRouteAsync);
}

async function fulfillClientApiRouteAsync(route: Route): Promise<void> {
    const { pathname } = new URL(route.request().url());
    if (route.request().method() === 'PUT' && pathname.endsWith('/recommendations/rec-1/read')) {
        await route.fulfill(jsonResponse(null));
        return;
    }

    await route.fulfill(jsonResponse(resolveClientApiResponse(pathname)));
}

function resolveClientApiResponse(pathname: string): unknown {
    const normalizedPathname = pathname.replace(/\/$/, '');
    const mock = CLIENT_API_MOCKS.find(item => item.matches(normalizedPathname));
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

function createRecommendations(): Array<Record<string, unknown>> {
    return [
        {
            id: 'rec-1',
            dietologistUserId: 'dietologist-1',
            dietologistFirstName: 'Ada',
            dietologistLastName: 'Lovelace',
            text: 'Add a protein source to breakfast.',
            isRead: false,
            createdAtUtc: '2026-05-01T10:00:00.000Z',
            readAtUtc: null,
        },
    ];
}

function createMealsOverview(): Record<string, unknown> {
    return {
        allConsumptions: {
            data: [
                createMeal('meal-1', '2026-05-07T20:40:00.000Z', [
                    createMealItem('meal-1-item-1', 'meal-1', 'Carrots', TEST_IMAGE_URLS[0]),
                    createMealItem('meal-1-item-2', 'meal-1', 'Rice', TEST_IMAGE_URLS[1]),
                    createMealItem('meal-1-item-3', 'meal-1', 'Salad', TEST_IMAGE_URLS[2]),
                    createMealItem('meal-1-item-4', 'meal-1', 'Soup', TEST_IMAGE_URLS[3]),
                ]),
                createMeal('meal-2', '2026-05-07T15:38:00.000Z', [
                    createMealItem('meal-2-item-1', 'meal-2', 'Rice', TEST_IMAGE_URLS[1]),
                    createMealItem('meal-2-item-2', 'meal-2', 'Salad', TEST_IMAGE_URLS[2]),
                ]),
            ],
            page: 1,
            limit: 20,
            totalPages: 1,
            totalItems: 2,
        },
        favoriteItems: [],
        favoriteTotalCount: 0,
    };
}

function createMeal(id: string, date: string, items: unknown[]): Record<string, unknown> {
    return {
        id,
        date,
        mealType: 'Dinner',
        comment: null,
        imageUrl: null,
        imageAssetId: null,
        totalCalories: 905,
        totalProteins: 58,
        totalFats: 45,
        totalCarbs: 66,
        totalFiber: 5,
        totalAlcohol: 0,
        isNutritionAutoCalculated: true,
        manualCalories: null,
        manualProteins: null,
        manualFats: null,
        manualCarbs: null,
        manualFiber: null,
        manualAlcohol: null,
        preMealSatietyLevel: null,
        postMealSatietyLevel: null,
        qualityScore: 34,
        qualityGrade: 'yellow',
        isFavorite: false,
        favoriteMealId: null,
        items,
        aiSessions: [],
    };
}

function createMealItem(id: string, consumptionId: string, productName: string, productImageUrl: string): Record<string, unknown> {
    return {
        id,
        consumptionId,
        amount: 100,
        productId: `${id}-product`,
        productName,
        productImageUrl,
        productBaseUnit: 'G',
        productBaseAmount: 100,
        productCaloriesPerBase: 100,
        productProteinsPerBase: 10,
        productFatsPerBase: 5,
        productCarbsPerBase: 15,
        productFiberPerBase: 2,
        productAlcoholPerBase: 0,
    };
}

function createSvgDataUrl(color: string, label: string): string {
    const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="160" height="160" viewBox="0 0 160 160"><rect width="160" height="160" fill="${color}"/><text x="80" y="94" text-anchor="middle" font-family="Arial" font-size="48" font-weight="700" fill="white">${label}</text></svg>`;
    return `data:image/svg+xml;base64,${Buffer.from(svg, 'utf8').toString('base64')}`;
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

function createProductsOverview(): Record<string, unknown> {
    return {
        allProducts: createProductsPage(),
        recentItems: [],
        favoriteItems: [],
        favoriteTotalCount: 0,
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
