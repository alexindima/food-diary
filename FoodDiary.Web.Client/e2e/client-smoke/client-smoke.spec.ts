const PRODUCT_NUTRIENT_FIELD_COUNT = 6;
import AxeBuilder from '@axe-core/playwright';
import { expect, type Locator, type Page, type Request, type Route, test } from '@playwright/test';

const MS_PER_SECOND = 1000;
const AUTH_TOKEN_TTL_SECONDS = 3600;
const SESSION_RESTORE_DELAY_MS = 750;
const ACCESSIBILITY_TEST_TIMEOUT_MS = 120_000;
const NETWORK_AUDIT_TEST_TIMEOUT_MS = 180_000;
const API_ERROR_STATUS_MIN = 400;
const NETWORK_AUDIT_DEFAULT_MAX_REQUESTS = 8;
const NETWORK_AUDIT_QUIET_MS = 500;
// These views compose independent sections in addition to the four shared shell requests.
const NETWORK_AUDIT_REQUEST_BUDGETS: Readonly<Record<string, number>> = {
    '/profile': 9,
    '/dietologist/clients/client-1': 10,
};
const ACCESSIBILITY_STABILITY_CSS = `
    *,
    *::before,
    *::after {
        animation: none !important;
        scroll-behavior: auto !important;
        transition: none !important;
    }
`;
const ACCESSIBILITY_ROUTES = [
    '/dashboard',
    '/products',
    '/products/add',
    '/meals',
    '/meals/add',
    '/recipes',
    '/recipes/add',
    '/explore',
    '/shopping-lists',
    '/goals',
    '/statistics',
    '/weight-history',
    '/waist-history',
    '/cycle-tracking',
    '/meal-plans',
    '/weekly-check-in',
    '/lessons',
    '/gamification',
    '/fasting',
    '/premium',
    '/profile',
    '/dietologist',
    '/recommendations',
] as const;
const NETWORK_AUDIT_ROUTES = [
    ...ACCESSIBILITY_ROUTES,
    '/products/p1/edit',
    '/meals/meal-1/edit',
    '/recipes/recipe-1/edit',
    '/meal-plans/plan-1',
    '/lessons/lesson-1',
    '/dietologist/clients/client-1',
    '/dietologist-invitations/invitation-1',
] as const;
const TEST_IMAGE_URLS = [
    createSvgDataUrl('#f97316', '1'),
    createSvgDataUrl('#22c55e', '2'),
    createSvgDataUrl('#0ea5e9', '3'),
    createSvgDataUrl('#a855f7', '4'),
] as const;
const CLIENT_API_MOCKS: readonly ClientApiMock[] = [
    { matches: pathname => pathname.endsWith('/users/info'), createResponse: createUser },
    { matches: pathname => pathname.endsWith('/users/overview'), createResponse: createUserOverview },
    { matches: pathname => pathname.endsWith('/auth/sessions'), createResponse: () => [] },
    { matches: pathname => pathname.endsWith('/billing/overview'), createResponse: createBillingOverview },
    { matches: pathname => pathname.endsWith('/recipes/explore'), createResponse: createEmptyProductsPage },
    { matches: pathname => pathname.endsWith('/meal-plans'), createResponse: () => [] },
    { matches: pathname => pathname.endsWith('/shopping-lists'), createResponse: () => [] },
    { matches: pathname => pathname.endsWith('/lessons'), createResponse: () => [] },
    { matches: pathname => pathname.endsWith('/statistics/summary'), createResponse: () => ({ nutrition: [], weight: [], waist: [] }) },
    { matches: pathname => pathname.endsWith('/weight-entries/page-summary'), createResponse: createWeightHistoryPageSummary },
    { matches: pathname => pathname.endsWith('/waist-entries/page-summary'), createResponse: createWaistHistoryPageSummary },
    { matches: pathname => pathname.endsWith('/dashboard'), createResponse: createDashboardSnapshot },
    { matches: pathname => pathname.endsWith('/meals/overview'), createResponse: createMealsOverview },
    { matches: pathname => pathname.endsWith('/fasting/overview'), createResponse: createFastingOverview },
    { matches: pathname => pathname.endsWith('/cycles/current'), createResponse: () => null },
    { matches: pathname => pathname.endsWith('/tdee/insight'), createResponse: createTdeeInsight },
    { matches: pathname => pathname.endsWith('/usda/daily-micronutrients'), createResponse: createDailyMicronutrients },
    { matches: pathname => pathname.endsWith('/notifications/unread-count'), createResponse: () => ({ count: 2 }) },
    { matches: pathname => pathname.endsWith('/notifications'), createResponse: () => [] },
    {
        matches: pathname => pathname.endsWith('/dietologist/invitations/invitation-1/current-user'),
        createResponse: createDietologistInvitation,
    },
    { matches: pathname => pathname.endsWith('/client-tasks'), createResponse: () => [] },
    { matches: pathname => pathname.endsWith('/recommendations/rec-1/comments'), createResponse: () => [] },
    { matches: pathname => pathname.endsWith('/recommendations'), createResponse: createRecommendations },
    { matches: pathname => pathname.endsWith('/favorite-products'), createResponse: createEmptyProductsPage },
    { matches: pathname => pathname.endsWith('/products/overview'), createResponse: createProductsOverview },
    { matches: pathname => pathname.endsWith('/products/search'), createResponse: createProductsPage },
    { matches: pathname => pathname.endsWith('/products'), createResponse: createProductsPage },
    { matches: pathname => pathname.endsWith('/products/p1'), createResponse: createOwnedProduct },
    { matches: pathname => pathname.endsWith('/meals/meal-1'), createResponse: () => createMeal('meal-1', '2026-04-19T18:00:00Z', []) },
    { matches: pathname => pathname.endsWith('/meal-plans/plan-1'), createResponse: createMealPlanDetail },
    { matches: pathname => pathname.endsWith('/lessons/lesson-1'), createResponse: createLessonDetail },
    { matches: pathname => pathname.endsWith('/recipes/recipe-1'), createResponse: createOwnedRecipe },
    { matches: pathname => pathname.endsWith('/dietologist/clients/attention'), createResponse: () => [] },
    { matches: pathname => pathname.endsWith('/dietologist/clients'), createResponse: createDietologistClients },
    { matches: pathname => pathname.endsWith('/dietologist/recommendation-templates'), createResponse: () => [] },
    { matches: pathname => pathname.endsWith('/dietologist/clients/client-1/dashboard'), createResponse: createDashboardSnapshot },
    {
        matches: pathname => pathname.endsWith('/dietologist/clients/client-1/goals'),
        createResponse: () => ({ id: 'client-1', email: 'client@example.test' }),
    },
    { matches: pathname => pathname.endsWith('/dietologist/clients/client-1/recommendations'), createResponse: () => [] },
    { matches: pathname => pathname.endsWith('/dietologist/clients/client-1/tasks'), createResponse: () => [] },
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
        await expect(page.getByRole('heading', { name: 'Meal for today' })).toBeVisible();
    });
});

test.describe('authenticated accessibility', () => {
    for (const viewport of [
        { name: 'desktop', width: 1440, height: 900 },
        { name: 'mobile', width: 390, height: 844 },
    ] as const) {
        test(`has no detectable WCAG A/AA violations on ${viewport.name} routes`, async ({ page }) => {
            test.setTimeout(ACCESSIBILITY_TEST_TIMEOUT_MS);
            const runtimeErrors: string[] = [];
            page.on('pageerror', error => runtimeErrors.push(error.message));
            page.on('console', message => {
                if (message.type() === 'error' && message.text().startsWith('ERROR')) {
                    runtimeErrors.push(message.text());
                }
            });
            await page.setViewportSize(viewport);
            await page.addInitScript(
                (tokens: { user: string; dietologist: string }) => {
                    const token = window.location.pathname === '/dietologist' ? tokens.dietologist : tokens.user;
                    window.localStorage.setItem('authToken', token);
                    window.localStorage.setItem('refreshToken', 'refresh-token');
                    window.localStorage.setItem('userId', 'u1');
                    window.localStorage.setItem('emailConfirmed', 'true');
                },
                { user: createAuthenticatedUserJwt(), dietologist: createAuthenticatedUserJwt('Dietologist') },
            );
            await mockAuthenticatedClientApiAsync(page);

            for (const route of ACCESSIBILITY_ROUTES) {
                await page.goto(route);
                await stabilizeAccessibilityPageAsync(page, route);
                await expect(page).toHaveURL(url => url.pathname === route);
                const results = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa', 'wcag21aa', 'wcag22aa']).analyze();

                expect(results.violations, `Accessibility violations on ${route} (${viewport.name})`).toEqual([]);
                expect.soft(runtimeErrors, `Runtime errors on ${route} (${viewport.name})`).toEqual([]);
                runtimeErrors.length = 0;
            }
        });
    }
});

test.describe('authenticated network audit', () => {
    test('@network-audit has no duplicate GET requests or failed API responses on initial route load', async ({ browser }, testInfo) => {
        test.setTimeout(NETWORK_AUDIT_TEST_TIMEOUT_MS);
        const report: NetworkAuditRouteResult[] = [];

        for (const routePath of NETWORK_AUDIT_ROUTES) {
            const page = await browser.newPage();
            const { requests, runtimeErrors, isSettled } = observeNetworkAudit(page);

            await authenticateUserAsync(page, routePath.startsWith('/dietologist') ? 'Dietologist' : 'User');
            await mockAuthenticatedClientApiAsync(page);
            await page.goto(routePath);
            await expect(page.locator('body')).toBeVisible();
            await expect(page.locator('html')).toHaveAttribute('data-i18n-ready', /^(?:en|ru)$/u);
            await expect(page.locator('main router-outlet + *').last()).toBeVisible();
            await expect(page).toHaveURL(url => url.pathname === routePath);
            await expect.poll(isSettled).toBe(true);
            expect.soft(runtimeErrors, `Runtime errors on ${routePath}`).toEqual([]);

            const duplicateGets = findDuplicateGetRequests(requests);
            const failedRequests = requests.filter(request => request.status === 0 || request.status >= API_ERROR_STATUS_MIN);
            report.push({
                route: routePath,
                requestCount: requests.length,
                uniqueRequestCount: new Set(requests.map(request => `${request.method} ${request.resource}`)).size,
                duplicateGets,
                failedRequests,
                requests,
            });

            await page.close();
        }

        await testInfo.attach('network-audit.json', {
            body: Buffer.from(JSON.stringify(report, null, 2), 'utf8'),
            contentType: 'application/json',
        });
        const reportTable = formatNetworkAuditTable(report);
        await testInfo.attach('network-audit.md', {
            body: Buffer.from(reportTable, 'utf8'),
            contentType: 'text/markdown',
        });
        process.stdout.write(`\n${reportTable}\n`);

        const routesWithDuplicateGets = report.filter(result => result.duplicateGets.length > 0);
        const routesWithFailedRequests = report.filter(result => result.failedRequests.length > 0);
        const routesOverRequestBudget = report.filter(
            result => result.requestCount > (NETWORK_AUDIT_REQUEST_BUDGETS[result.route] ?? NETWORK_AUDIT_DEFAULT_MAX_REQUESTS),
        );
        expect.soft(routesWithDuplicateGets, formatNetworkAuditFailures('Duplicate GET requests', routesWithDuplicateGets)).toEqual([]);
        expect.soft(routesWithFailedRequests, formatNetworkAuditFailures('Failed API requests', routesWithFailedRequests)).toEqual([]);
        expect
            .soft(routesOverRequestBudget, formatNetworkAuditFailures('API request budget exceeded', routesOverRequestBudget))
            .toEqual([]);
    });
});

function observeNetworkAudit(page: Page): {
    requests: NetworkAuditRequest[];
    runtimeErrors: string[];
    isSettled: () => boolean;
} {
    const requests: NetworkAuditRequest[] = [];
    const runtimeErrors: string[] = [];
    page.on('pageerror', error => runtimeErrors.push(error.message));
    page.on('console', message => {
        if (message.type() === 'error' && message.text().startsWith('ERROR')) {
            runtimeErrors.push(message.text());
        }
    });
    const pendingRequests = new Set<Request>();
    let lastApiActivity = Date.now();
    page.on('request', request => {
        if (new URL(request.url()).pathname.startsWith('/api/v1/')) {
            pendingRequests.add(request);
            lastApiActivity = Date.now();
        }
    });
    page.on('requestfinished', request => {
        if (pendingRequests.delete(request)) {
            lastApiActivity = Date.now();
        }
    });
    page.on('requestfailed', request => {
        if (pendingRequests.delete(request)) {
            lastApiActivity = Date.now();
            requests.push({ method: request.method(), resource: normalizeAuditResource(new URL(request.url())), status: 0 });
        }
    });
    page.on('response', response => {
        const request = response.request();
        const url = new URL(request.url());
        if (!url.pathname.startsWith('/api/v1/')) {
            return;
        }

        requests.push({
            method: request.method(),
            resource: normalizeAuditResource(url),
            status: response.status(),
        });
    });
    return {
        requests,
        runtimeErrors,
        isSettled: () => pendingRequests.size === 0 && Date.now() - lastApiActivity >= NETWORK_AUDIT_QUIET_MS,
    };
}

async function stabilizeAccessibilityPageAsync(page: Page, route: (typeof ACCESSIBILITY_ROUTES)[number]): Promise<void> {
    await expect(page.locator('body')).toBeVisible();
    await expect(page.locator('html')).toHaveAttribute('data-i18n-ready', /^(?:en|ru)$/u);
    if (route === '/dashboard') {
        await expect(page.getByRole('textbox', { name: 'Describe your meal, e.g. "two eggs and toast"...', exact: true })).toBeVisible();
    }
    if (route === '/fasting') {
        await expect(page.locator('.fd-ui-progress-ring')).toHaveAttribute('aria-label', /\S+/u);
        await expect(page.locator('.fasting-redesign__history-action > button')).toHaveAccessibleName(/\S+/u);
    }

    await page.addStyleTag({ content: ACCESSIBILITY_STABILITY_CSS });
    await page.evaluate(async () => {
        await new Promise<void>(resolve => {
            requestAnimationFrame(resolve);
        });
        await new Promise<void>(resolve => {
            requestAnimationFrame(resolve);
        });
    });
}

function normalizeAuditResource(url: URL): string {
    const sortedSearchParams = [...url.searchParams.entries()].sort(([leftKey, leftValue], [rightKey, rightValue]) =>
        `${leftKey}=${leftValue}`.localeCompare(`${rightKey}=${rightValue}`),
    );
    const query = new URLSearchParams(sortedSearchParams).toString();
    return query.length === 0 ? url.pathname : `${url.pathname}?${query}`;
}

function findDuplicateGetRequests(requests: readonly NetworkAuditRequest[]): string[] {
    const counts = new Map<string, number>();
    for (const request of requests) {
        if (request.method !== 'GET') {
            continue;
        }

        counts.set(request.resource, (counts.get(request.resource) ?? 0) + 1);
    }

    return [...counts.entries()]
        .filter(([, count]) => count > 1)
        .map(([resource, count]) => `${resource} ×${count}`)
        .sort((left, right) => left.localeCompare(right));
}

function formatNetworkAuditFailures(title: string, results: readonly NetworkAuditRouteResult[]): string {
    const details = results.map(result => {
        let issues: string;
        if (title.startsWith('Duplicate')) {
            issues = result.duplicateGets.join(', ');
        } else if (title.startsWith('Failed')) {
            issues = result.failedRequests.map(request => `${request.method} ${request.resource} (${request.status})`).join(', ');
        } else {
            issues = `${result.requestCount} requests (budget ${NETWORK_AUDIT_REQUEST_BUDGETS[result.route] ?? NETWORK_AUDIT_DEFAULT_MAX_REQUESTS})`;
        }
        return `${result.route}: ${issues}`;
    });
    return `${title}\n${details.join('\n')}`;
}

function formatNetworkAuditTable(results: readonly NetworkAuditRouteResult[]): string {
    const header = '| Route | Requests | Unique | Duplicates | API resources |';
    const separator = '| --- | ---: | ---: | --- | --- |';
    const rows = results.map(result => {
        const resources = result.requests.map(request => `${request.method} ${request.resource}`).join('<br>');
        const duplicates = result.duplicateGets.join('<br>');
        return `| ${result.route} | ${result.requestCount} | ${result.uniqueRequestCount} | ${duplicates === '' ? '—' : duplicates} | ${resources === '' ? '—' : resources} |`;
    });
    return [header, separator, ...rows].join('\n');
}

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
    test('renders privacy policy without the authenticated shell', async ({ page }) => {
        await page.addInitScript((token: string) => {
            window.localStorage.setItem('authToken', token);
            window.localStorage.setItem('refreshToken', 'refresh-token');
            window.localStorage.setItem('userId', 'u1');
            window.localStorage.setItem('emailConfirmed', 'true');
        }, createAuthenticatedUserJwt());

        await mockAuthenticatedClientApiAsync(page);
        await page.goto('/privacy-policy');

        await expect(page.getByRole('heading', { level: 1 })).toContainText('Privacy Policy');
        await expect(page.locator('.fd-root')).toHaveClass(/fd-root--no-sidebar/);
        await expect(page.locator('fd-sidebar')).toHaveCount(0);
        await expect(page.locator('fd-quick-meal-drawer')).toHaveCount(0);
    });

    test('renders not found page for unknown route', async ({ page }) => {
        await page.goto('/missing-page');

        await expect(page).toHaveURL(/\/missing-page$/);
        await expect(page.getByRole('heading')).toContainText('Page not found');
        await expect(page.getByText('block', { exact: true })).toHaveCount(0);
        await page.getByRole('button', { name: 'Open the food diary' }).click();
        await expect(page).toHaveURL(/\/food-diary$/);
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
        await expect(page.getByRole('button', { name: 'Create', exact: true })).toBeVisible();
        await page.getByRole('button', { name: 'More actions' }).click();
        await expect(page.getByRole('menuitem', { name: 'Create' })).toHaveCount(0);
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

test.describe('deterministic authenticated feature fixtures', () => {
    test('renders deterministic meal plan detail data', async ({ page }) => {
        await authenticateUserAsync(page);
        await mockAuthenticatedClientApiAsync(page);

        await page.goto('/meal-plans/plan-1');

        await expect(page).toHaveURL(/\/meal-plans\/plan-1$/);
        await expect(page.getByRole('heading', { name: 'Balanced week', level: 1 })).toBeVisible();
        await expect(page.getByText('Greek yogurt breakfast')).toBeVisible();
        await expect(page.getByRole('button', { name: /Generate shopping list/ })).toBeVisible();
    });

    test('renders deterministic lesson detail and marks it read', async ({ page }) => {
        await authenticateUserAsync(page);
        await mockAuthenticatedClientApiAsync(page);

        await page.goto('/lessons/lesson-1');

        await expect(page).toHaveURL(/\/lessons\/lesson-1$/);
        await expect(page.getByRole('heading', { name: 'Build a balanced plate', level: 2 })).toBeVisible();
        await expect(page.getByText('Use vegetables, protein, and whole grains as a practical starting point.')).toBeVisible();
        await page.getByRole('button', { name: /Mark as read/ }).click();
        await expect(page.getByText(/Already read/)).toBeVisible();
    });

    test('hydrates the owned recipe edit form from a deterministic fixture', async ({ page }) => {
        await authenticateUserAsync(page);
        await mockAuthenticatedClientApiAsync(page);

        await page.goto('/recipes/recipe-1/edit');

        await expect(page).toHaveURL(/\/recipes\/recipe-1\/edit$/);
        await expect(page.getByLabel('Name', { exact: true })).toHaveValue('Roasted vegetable bowl');
        await expect(page.getByLabel('Servings', { exact: true })).toHaveValue('2');
        await expect(page.getByText('Roast the vegetables')).toBeVisible();
    });

    test('renders the dietologist client list with a role-specific fixture', async ({ page }) => {
        await authenticateUserAsync(page, 'Dietologist');
        await mockAuthenticatedClientApiAsync(page);

        await page.goto('/dietologist');

        await expect(page).toHaveURL(/\/dietologist$/);
        await expect(page.getByRole('heading', { name: 'My clients', level: 1 })).toBeVisible();
        await expect(page.getByRole('button', { name: /Taylor Example/ })).toBeVisible();
        await expect(page.getByText('client@example.test')).toBeVisible();
    });
});

const DASHBOARD_NARROW_WIDTH = 360;
const DASHBOARD_MOBILE_WIDTH = 390;
const DASHBOARD_DESKTOP_WIDTH = 1440;
const DASHBOARD_TEST_WIDTHS = [DASHBOARD_NARROW_WIDTH, DASHBOARD_MOBILE_WIDTH, DASHBOARD_DESKTOP_WIDTH] as const;
const DASHBOARD_WATER_ACTIONS = 3;
const DASHBOARD_WEEK_DAYS = 7;
const DASHBOARD_WEEK_START_DAY = 13;
const DASHBOARD_RECORDED_CALORIES = 1800;
const DASHBOARD_PREVIOUS_DAY_INDEX = 5;
const DASHBOARD_WEIGHT_START = 79.2;
const DASHBOARD_WEIGHT_MIDDLE = 78.7;
const DASHBOARD_WEIGHT_END = 78;
const DASHBOARD_WEIGHT_VALUES = [DASHBOARD_WEIGHT_START, DASHBOARD_WEIGHT_MIDDLE, DASHBOARD_WEIGHT_END] as const;
const DASHBOARD_WAIST_START = 82;
const DASHBOARD_WAIST_MIDDLE = 81.5;
const DASHBOARD_WAIST_END = 81;
const DASHBOARD_WAIST_VALUES = [DASHBOARD_WAIST_START, DASHBOARD_WAIST_MIDDLE, DASHBOARD_WAIST_END] as const;
const DASHBOARD_TREND_START_DAY = 17;
const DASHBOARD_FAST_HOURS = 16;
const DASHBOARD_DAY_HOURS = 24;
const DASHBOARD_CYCLE_EAT_DAYS = 3;

// Dashboard regressions run with intercepted APIs; no local diary is changed.
test.describe('dashboard regression', () => {
    test.use({ timezoneId: 'UTC' });
    test.beforeEach(async ({ page }) => {
        await page.clock.install({ time: new Date('2026-04-19T12:00:00Z') });
        await authenticateUserAsync(page);
        await mockAuthenticatedClientApiAsync(page);
    });

    for (const width of DASHBOARD_TEST_WIDTHS) {
        for (const phase of ['Intermittent', 'Extended', 'Cyclic'] as const) {
            test(`${phase} at ${width}px keeps current marker clear of the timer`, async ({ page }) => {
                await page.setViewportSize({ width, height: 900 });
                const snapshot = createDashboardRegressionSnapshot();
                snapshot['currentFastingSession'] = createDashboardRegressionFast(phase);
                await page.route(/\/api\/v1\/dashboard\/?(?:\?|$)/u, async route => route.fulfill(jsonResponse(snapshot)));
                await page.goto('/dashboard');
                const card = page.locator('fd-dashboard-fasting-card');
                await expect(card.locator('.dashboard-fasting-card__elapsed')).toHaveText(/^01:00:\d{2}$/u);
                const marker = await dashboardBoundsAsync(card.locator('.dashboard-fasting-card__now'));
                const timer = await dashboardBoundsAsync(card.locator('.dashboard-fasting-card__elapsed'));
                expect(marker).not.toBeNull();
                expect(timer).not.toBeNull();
                expect(timer.y).toBeGreaterThanOrEqual(marker.y + marker.height);
                const summary = page.locator('fd-dashboard-summary-block');
                expect(
                    await card.evaluate(
                        (element, next) => Boolean(element.compareDocumentPosition(next as Node) & Node.DOCUMENT_POSITION_FOLLOWING),
                        await summary.elementHandle(),
                    ),
                ).toBe(true);
                if (phase === 'Cyclic') {
                    await expect(card.locator('[aria-current="step"]')).toHaveCount(1);
                    await expect(card.locator('[aria-current="step"]')).toBeInViewport();
                }
                await expectNoHorizontalOverflowAsync(page);
            });
        }
    }
});

test.describe('dashboard regression eating phases', () => {
    test.use({ timezoneId: 'UTC' });
    for (const phase of ['Intermittent', 'Cyclic'] as const) {
        test(`${phase} eating state keeps summary first and shows the current cycle day on mobile`, async ({ page }) => {
            await page.clock.install({ time: new Date('2026-04-19T12:00:00Z') });
            await authenticateUserAsync(page);
            await mockAuthenticatedClientApiAsync(page);
            await page.setViewportSize({ width: 390, height: 844 });
            const snapshot = createDashboardRegressionSnapshot();
            snapshot['currentFastingSession'] = {
                ...createDashboardRegressionFast(phase),
                startedAtUtc: '2026-04-18T18:00:00Z',
                occurrenceKind: phase === 'Cyclic' ? 'EatDay' : 'FastingWindow',
                cyclicPhaseDayNumber: 2,
                cyclicPhaseDayTotal: 3,
            };
            await page.route(/\/api\/v1\/dashboard\/?(?:\?|$)/u, async route => route.fulfill(jsonResponse(snapshot)));
            await page.goto('/dashboard');
            await page.locator('fd-dashboard-fasting-block').scrollIntoViewIfNeeded();
            const card = page.locator('fd-dashboard-fasting-card');
            await expect(card.locator('.dashboard-fasting-card-shell--eating')).toBeVisible();
            await expect(card.locator('.dashboard-fasting-card__stage')).toContainText('Eating');
            await expect(page.locator('.dashboard__column').first().locator(':scope > :first-child')).toHaveJSProperty(
                'tagName',
                'FD-DASHBOARD-SUMMARY-BLOCK',
            );
            if (phase === 'Cyclic') {
                await card.locator('.dashboard-fasting-card__days').scrollIntoViewIfNeeded();
                await expect(card.locator('[aria-current="step"]')).toContainText('Day 3');
                await expect(card.locator('[aria-current="step"]')).toBeInViewport({ ratio: 1 });
            } else {
                await expect(card.locator('.dashboard-fasting-card__elapsed')).toHaveText(/^02:00:\d{2}$/u);
            }
            await expectNoHorizontalOverflowAsync(page);
        });
    }
});

test.describe('dashboard regression interactions', () => {
    test.use({ timezoneId: 'UTC' });
    test.beforeEach(async ({ page }) => {
        await page.clock.install({ time: new Date('2026-04-19T12:00:00Z') });
        await authenticateUserAsync(page);
        await mockAuthenticatedClientApiAsync(page);
    });
    test('mobile today has one water card before meals; historical dates disable quick additions', async ({ page }) => {
        await page.setViewportSize({ width: 390, height: 844 });
        await page.route(/\/api\/v1\/dashboard\/?(?:\?|$)/u, async route =>
            route.fulfill(jsonResponse(createDashboardRegressionSnapshot())),
        );
        await page.goto('/dashboard');
        await page.locator('fd-dashboard-hydration-block').scrollIntoViewIfNeeded();
        await expect(page.locator('fd-hydration-card')).toHaveCount(1);
        await expect(page.locator('.dashboard__column:not(.dashboard__column--aside) fd-hydration-card')).toBeVisible();
        await expect(page.locator('fd-hydration-card .hydration-card__quick-action')).toHaveCount(DASHBOARD_WATER_ACTIONS);
        await page.goto('/dashboard?date=2026-04-18');
        await expect(page.locator('fd-dashboard-quick-add')).toHaveCount(0);
        await page.locator('fd-dashboard-hydration-block').scrollIntoViewIfNeeded();
        await expect(page.locator('fd-hydration-card')).toHaveCount(1);
        await page.locator('fd-dashboard-hydration-block').scrollIntoViewIfNeeded();
        await expect(page.locator('fd-hydration-card .hydration-card__quick-action')).toHaveCount(0);
        await page.locator('fd-dashboard-tdee-block').scrollIntoViewIfNeeded();
        await expect(page.locator('fd-tdee-insight-card')).toContainText('current', { ignoreCase: true });
    });

    test('the weekly chart preserves desktop composition for both populated and empty days and opens the selected date', async ({
        page,
    }) => {
        await page.setViewportSize({ width: 1440, height: 1000 });
        await page.route(/\/api\/v1\/dashboard\/?(?:\?|$)/u, async route =>
            route.fulfill(jsonResponse(createDashboardRegressionSnapshot())),
        );
        await page.goto('/dashboard');
        const chart = page.locator('fd-nutrition-weekly-trend-card');
        await chart.scrollIntoViewIfNeeded();
        const choices = chart.locator('[aria-pressed]');
        await expect(choices).toHaveCount(DASHBOARD_WEEK_DAYS);
        await expect(chart.locator('.nutrition-trend__open-day')).toBeHidden();
        const original = await dashboardBoundsAsync(chart);
        await choices.nth(DASHBOARD_PREVIOUS_DAY_INDEX).click();
        await expect(chart.locator('.nutrition-trend__day-calories')).toContainText('1,800');
        const populated = await dashboardBoundsAsync(chart);
        await choices.first().click();
        await expect(chart.locator('.nutrition-trend__day-calories')).toContainText('0');
        const empty = await dashboardBoundsAsync(chart);
        expect(Math.abs(populated.height - empty.height)).toBeLessThanOrEqual(2);
        expect(Math.abs(original.width - empty.width)).toBeLessThanOrEqual(2);
        const detail = await dashboardBoundsAsync(chart.locator('.nutrition-trend__day'));
        const bars = await dashboardBoundsAsync(chart.locator('.nutrition-trend__chart'));
        expect(detail.x).toBeGreaterThan(bars.x);
        await chart.locator('.nutrition-trend__open-day').click();
        await expect(page).toHaveURL(/date=2026-04-13/);
        await expect(page.locator('fd-dashboard-quick-add')).toHaveCount(0);
        await page.goBack();
        await expect(page.locator('fd-dashboard-quick-add')).toBeVisible();
    });
});

test.describe('dashboard regression measurement charts', () => {
    test.use({ timezoneId: 'UTC' });
    test.beforeEach(async ({ page }) => {
        await page.clock.install({ time: new Date('2026-04-19T12:00:00Z') });
        await authenticateUserAsync(page);
        await mockAuthenticatedClientApiAsync(page);
    });
    for (const kind of ['weight', 'waist'] as const) {
        test(`${kind} chart aligns with metrics and opens its history with the keyboard`, async ({ page }) => {
            await page.setViewportSize({ width: 1440, height: 1000 });
            await page.route(/\/api\/v1\/dashboard\/?(?:\?|$)/u, async route =>
                route.fulfill(jsonResponse(createDashboardRegressionSnapshot())),
            );
            await page.goto('/dashboard');
            const block = page.locator('fd-dashboard-trend-block').nth(kind === 'weight' ? 0 : 1);
            await block.scrollIntoViewIfNeeded();
            const chart = block.locator('.weight-trend-card__chart');
            await expect(chart).toBeVisible();
            const bounds = await dashboardBoundsAsync(chart);
            const metrics = await dashboardBoundsAsync(block.locator('.weight-trend-card__metrics'));
            expect(Math.abs(bounds.x - metrics.x)).toBeLessThanOrEqual(1);
            expect(Math.abs(bounds.width - metrics.width)).toBeLessThanOrEqual(1);
            if (kind === 'weight') {
                const goal = block.locator('.weight-trend-card__goal');
                await expect(goal).toBeVisible();
                const goalBounds = await dashboardBoundsAsync(goal);
                expect(goalBounds.y).toBeGreaterThanOrEqual(bounds.y + bounds.height);
                await expect(block.locator('.fd-ui-line-chart__reference-label')).toHaveCount(0);
            }
            const action = block.getByRole('button').first();
            await action.focus();
            await action.press('Enter');
            await expect(page).toHaveURL(kind === 'weight' ? /\/weight-history$/ : /\/waist-history$/);
        });
    }
});

test.describe('dashboard regression writes', () => {
    test.use({ timezoneId: 'UTC' });
    test.beforeEach(async ({ page }) => {
        await page.clock.install({ time: new Date('2026-04-19T12:00:00Z') });
        await authenticateUserAsync(page);
        await mockAuthenticatedClientApiAsync(page);
    });

    test('adding water sends the selected amount once and refreshes the displayed total', async ({ page }) => {
        const snapshot = createDashboardRegressionSnapshot();
        const requests: unknown[] = [];
        await page.route(/\/api\/v1\/dashboard\/?(?:\?|$)/u, async route => route.fulfill(jsonResponse(snapshot)));
        await page.route(/\/api\/v1\/hydrations\/?$/u, async route => {
            requests.push(route.request().postDataJSON());
            snapshot['hydration'] = { goalMl: 2200, totalMl: 1050, entries: [] };
            await route.fulfill(jsonResponse({ id: 'water-added' }));
        });
        await page.goto('/dashboard');
        await page.locator('fd-dashboard-hydration-block').scrollIntoViewIfNeeded();
        const card = page.locator('fd-hydration-card');
        await expect(card.locator('.hydration-card__total')).toHaveText('800');
        await card.getByRole('button', { name: /250/u }).click();
        await expect(card.locator('.hydration-card__total')).toHaveText('1,050');
        expect(requests).toEqual([expect.objectContaining({ amountMl: 250 })]);
    });

    test('applying a calculated goal updates the target without opening the details dialog', async ({ page }) => {
        const snapshot = createDashboardRegressionSnapshot();
        const insight = { ...createTdeeInsight(), suggestedCalorieTarget: 2100 };
        snapshot['tdeeInsight'] = insight;
        const requests: unknown[] = [];
        await page.route(/\/api\/v1\/dashboard\/?(?:\?|$)/u, async route => route.fulfill(jsonResponse(snapshot)));
        await page.route(/\/api\/v1\/goals\/?$/u, async route => {
            requests.push(route.request().postDataJSON());
            snapshot['dailyGoal'] = 2100;
            insight['currentCalorieTarget'] = 2100;
            await route.fulfill(jsonResponse({ dailyCalorieTarget: 2100 }));
        });
        await page.goto('/dashboard');
        await page.locator('fd-dashboard-tdee-block').scrollIntoViewIfNeeded();
        const apply = page.locator('fd-tdee-insight-card').getByRole('button', { name: 'Apply', exact: true });
        await apply.click();
        await expect(apply).toHaveCount(0);
        expect(requests).toEqual([{ dailyCalorieTarget: 2100 }]);
        await expect(page.locator('fd-tdee-insight-dialog')).toHaveCount(0);
        await expect(page.locator('.day-summary__goal-label')).toContainText('2,100');
    });

    test('layout mode toggles a measurement card without navigating to history or saving prematurely', async ({ page }) => {
        const writes: string[] = [];
        page.on('request', request => {
            if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(request.method()) && request.url().includes('/api/v1/users/')) {
                writes.push(request.url());
            }
        });
        await page.route(/\/api\/v1\/dashboard\/?(?:\?|$)/u, async route =>
            route.fulfill(jsonResponse(createDashboardRegressionSnapshot())),
        );
        await page.setViewportSize({ width: 1440, height: 1000 });
        await page.goto('/dashboard');
        await page.locator('.dashboard__settings-action button').click();
        await expect(page.locator('.dashboard--editing')).toBeVisible();
        const block = page.locator('fd-dashboard-trend-block').first().locator('[role="button"]');
        await block.click();
        await expect(block).toHaveAttribute('aria-pressed', 'false');
        await expect(page).toHaveURL(/\/dashboard$/u);
        await expect(page.locator('fd-dashboard-quick-add')).toHaveCount(0);
        expect(writes).toEqual([]);
    });
});

function createDashboardRegressionSnapshot(): Record<string, unknown> {
    return {
        ...createDashboardSnapshot(),
        tdeeInsight: createTdeeInsight(),
        weight: { latest: { date: '2026-04-19', weightKg: 78 }, previous: { date: '2026-04-12', weightKg: 79.2 }, desiredWeightKg: 75 },
        waist: {
            latest: { date: '2026-04-19', circumferenceCm: 81 },
            previous: { date: '2026-04-12', circumferenceCm: 82 },
            desiredWaistCm: 76,
        },
        weightTrend: DASHBOARD_WEIGHT_VALUES.map((value, index) => ({
            startDate: `2026-04-${DASHBOARD_TREND_START_DAY + index}`,
            averageWeightKg: value,
        })),
        waistTrend: DASHBOARD_WAIST_VALUES.map((value, index) => ({
            startDate: `2026-04-${DASHBOARD_TREND_START_DAY + index}`,
            averageCircumferenceCm: value,
        })),
        weeklyCalories: Array.from({ length: DASHBOARD_WEEK_DAYS }, (_, index) => ({
            date: `2026-04-${DASHBOARD_WEEK_START_DAY + index}`,
            calories: index === DASHBOARD_PREVIOUS_DAY_INDEX ? DASHBOARD_RECORDED_CALORIES : 0,
            proteins: 100,
            fats: 60,
            carbs: 190,
            fiber: 20,
        })),
    };
}

function createDashboardRegressionFast(planType: 'Intermittent' | 'Extended' | 'Cyclic'): Record<string, unknown> {
    return {
        id: 'dashboard-fast',
        startedAtUtc: '2026-04-19T11:00:00Z',
        endedAtUtc: null,
        initialPlannedDurationHours: planType === 'Intermittent' ? DASHBOARD_FAST_HOURS : DASHBOARD_DAY_HOURS,
        plannedDurationHours: planType === 'Intermittent' ? DASHBOARD_FAST_HOURS : DASHBOARD_DAY_HOURS,
        addedDurationHours: 0,
        protocol: 'Custom',
        planType,
        occurrenceKind: planType === 'Cyclic' ? 'FastDay' : 'FastingWindow',
        cyclicFastDays: planType === 'Cyclic' ? 1 : null,
        cyclicEatDays: planType === 'Cyclic' ? DASHBOARD_CYCLE_EAT_DAYS : null,
        cyclicPhaseDayNumber: planType === 'Cyclic' ? 1 : null,
        cyclicPhaseDayTotal: planType === 'Cyclic' ? 1 : null,
        cyclicEatDayFastHours: 16,
        cyclicEatDayEatingWindowHours: 8,
        isCompleted: false,
        status: 'Active',
        notes: null,
        checkIns: [],
        symptoms: [],
    };
}

async function authenticateUserAsync(page: Page, role = 'User'): Promise<void> {
    await page.addInitScript((token: string) => {
        window.localStorage.setItem('authToken', token);
        window.localStorage.setItem('refreshToken', 'refresh-token');
        window.localStorage.setItem('userId', 'u1');
        window.localStorage.setItem('emailConfirmed', 'true');
    }, createAuthenticatedUserJwt(role));
}

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
    if (route.request().method() === 'POST' && pathname.endsWith('/lessons/lesson-1/read')) {
        await route.fulfill({ status: 204 });
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
        theme: 'dark',
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

function createUserOverview(): Record<string, unknown> {
    return {
        user: createUser(),
        notificationPreferences: {
            pushNotificationsEnabled: true,
            fastingPushNotificationsEnabled: true,
            socialPushNotificationsEnabled: false,
            fastingCheckInReminderHours: 4,
            fastingCheckInFollowUpReminderHours: 2,
        },
        webPushSubscriptions: [],
        dietologistRelationship: null,
    };
}

function createBillingOverview(): Record<string, unknown> {
    return {
        isPremium: false,
        subscriptionStatus: null,
        plan: null,
        subscriptionProvider: null,
        currentPeriodStartUtc: null,
        currentPeriodEndUtc: null,
        nextBillingAttemptUtc: null,
        cancelAtPeriodEnd: false,
        renewalEnabled: false,
        manageBillingAvailable: false,
        premiumTrialStartUtc: null,
        premiumTrialEndUtc: null,
        premiumTrialActive: false,
        premiumTrialUsed: false,
        canStartPremiumTrial: true,
        provider: 'Paddle',
        paddleClientToken: null,
        availableProviders: [],
    };
}

function createFastingOverview(): Record<string, unknown> {
    return {
        currentSession: null,
        stats: {
            totalCompleted: 0,
            currentStreak: 0,
            averageDurationHours: 0,
            completionRateLast30Days: 0,
            checkInRateLast30Days: 0,
            lastCheckInAtUtc: null,
            topSymptom: null,
        },
        insights: { alerts: [], insights: [] },
        history: { data: [], page: 1, limit: 10, totalPages: 0, totalItems: 0 },
    };
}

function createWeightHistoryPageSummary(): Record<string, unknown> {
    return {
        entries: [],
        summary: [],
        height: 175,
        goal: { desiredWeight: null, startWeight: null, startedAtUtc: null },
        goalHistory: [],
    };
}

function createWaistHistoryPageSummary(): Record<string, unknown> {
    return {
        entries: [],
        summary: [],
        height: 175,
        goal: { desiredWaist: null, startWaist: null, startedAtUtc: null },
        goalHistory: [],
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
        allMeals: {
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

function createMealItem(id: string, mealId: string, productName: string, productImageUrl: string): Record<string, unknown> {
    return {
        id,
        mealId,
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

function createOwnedProduct(): Record<string, unknown> {
    return {
        id: 'p1',
        name: 'Greek yogurt',
        brand: 'Food Diary',
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

function createMealPlanDetail(): Record<string, unknown> {
    return {
        id: 'plan-1',
        name: 'Balanced week',
        description: 'A repeatable seven-day plan for deterministic visual checks.',
        dietType: 'Balanced',
        durationDays: 7,
        targetCaloriesPerDay: 1900,
        isCurated: true,
        days: [
            {
                id: 'plan-day-1',
                dayNumber: 1,
                meals: [
                    {
                        id: 'plan-meal-1',
                        mealType: 'Breakfast',
                        recipeId: 'recipe-1',
                        recipeName: 'Greek yogurt breakfast',
                        servings: 1,
                        calories: 420,
                        proteins: 32,
                        fats: 12,
                        carbs: 46,
                    },
                ],
            },
        ],
    };
}

function createLessonDetail(): Record<string, unknown> {
    return {
        id: 'lesson-1',
        title: 'Build a balanced plate',
        content: 'Use vegetables, protein, and whole grains as a practical starting point.',
        summary: 'A simple composition guide.',
        category: 'NutritionBasics',
        difficulty: 'Beginner',
        estimatedReadMinutes: 4,
        isRead: false,
    };
}

function createOwnedRecipe(): Record<string, unknown> {
    return {
        id: 'recipe-1',
        name: 'Roasted vegetable bowl',
        description: 'A deterministic recipe used for edit-flow checks.',
        comment: null,
        category: 'Dinner',
        imageUrl: null,
        imageAssetId: null,
        prepTime: 15,
        cookTime: 30,
        servings: 2,
        visibility: 'Private',
        usageCount: 0,
        createdAt: '2026-07-01T10:00:00.000Z',
        isOwnedByCurrentUser: true,
        qualityScore: 85,
        qualityGrade: 'green',
        totalCalories: 640,
        totalProteins: 24,
        totalFats: 18,
        totalCarbs: 92,
        totalFiber: 16,
        totalAlcohol: 0,
        isNutritionAutoCalculated: true,
        manualCalories: null,
        manualProteins: null,
        manualFats: null,
        manualCarbs: null,
        manualFiber: null,
        manualAlcohol: null,
        isFavorite: false,
        favoriteRecipeId: null,
        steps: [
            {
                id: 'recipe-step-1',
                stepNumber: 1,
                title: 'Roast the vegetables',
                instruction: 'Roast until tender and lightly browned.',
                imageUrl: null,
                imageAssetId: null,
                ingredients: [],
            },
        ],
    };
}

function createDietologistClients(): Array<Record<string, unknown>> {
    return [
        {
            userId: 'client-1',
            email: 'client@example.test',
            firstName: 'Taylor',
            lastName: 'Example',
            profileImage: null,
            birthDate: '1992-04-12',
            gender: 'Other',
            height: 172,
            activityLevel: 'Moderate',
            permissions: {
                shareProfile: true,
                shareMeals: true,
                shareStatistics: true,
                shareWeight: true,
                shareWaist: true,
                shareGoals: true,
                shareHydration: true,
                shareFasting: true,
            },
            acceptedAtUtc: '2026-07-01T10:00:00.000Z',
        },
    ];
}

function createDietologistInvitation(): Record<string, unknown> {
    return {
        invitationId: 'invitation-1',
        clientUserId: 'client-1',
        clientEmail: 'client@example.test',
        clientFirstName: 'Taylor',
        clientLastName: 'Example',
        status: 'Pending',
        createdAtUtc: '2026-07-01T10:00:00.000Z',
        expiresAtUtc: '2026-07-08T10:00:00.000Z',
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

function createAuthenticatedUserJwt(role = 'User'): string {
    return createJwt({
        sub: 'u1',
        nameid: 'u1',
        role,
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

type NetworkAuditRequest = {
    method: string;
    resource: string;
    status: number;
};

type NetworkAuditRouteResult = {
    route: string;
    requestCount: number;
    uniqueRequestCount: number;
    duplicateGets: string[];
    failedRequests: NetworkAuditRequest[];
    requests: NetworkAuditRequest[];
};

async function dashboardBoundsAsync(locator: Locator): Promise<{ x: number; y: number; width: number; height: number }> {
    const bounds = await locator.boundingBox();
    if (bounds === null) {
        throw new Error(`Expected a visible dashboard element: ${locator.toString()}`);
    }
    return bounds;
}

test.describe('dashboard TDEE accessibility regression', () => {
    for (const key of ['Enter', 'Space']) {
        test(`opens details once with ${key} while help remains a separate control`, async ({ page }) => {
            await authenticateUserAsync(page);
            await mockAuthenticatedClientApiAsync(page);
            await page.goto('/dashboard');
            const block = page.locator('fd-dashboard-tdee-block');
            await block.scrollIntoViewIfNeeded();
            await expect(block.locator('fd-tdee-insight-card')).toBeVisible();
            const help = block.locator('fd-ui-button[icon="info"] button');
            await help.click();
            await expect(page.locator('fd-tdee-insight-dialog')).toHaveCount(0);
            const results = await new AxeBuilder({ page }).include('fd-dashboard-tdee-block').analyze();
            expect(results.violations).toEqual([]);
            const open = block.getByRole('button', { name: 'Energy expenditure', exact: true });
            await open.focus();
            await expect(open).toBeFocused();
            await open.press(key);
            await expect(page.locator('fd-tdee-insight-dialog')).toHaveCount(1);
        });
    }
});

const FAVORITES_MOBILE_WIDTH = 390;
const FAVORITES_DESKTOP_WIDTH = 1280;
const FAVORITES_PAGE_SIZE = 10;
const FAVORITES_SEARCH_MATCHES = 4;
const RESTORE_PATH_OFFSET = -2;

test.describe('meal favorites regression', () => {
    for (const width of [FAVORITES_MOBILE_WIDTH, FAVORITES_DESKTOP_WIDTH]) {
        test(`independent inline undo, retry and pagination at ${width}px`, async ({ page }) => {
            await page.setViewportSize({ width, height: 900 });
            await authenticateUserAsync(page);
            await mockAuthenticatedClientApiAsync(page);
            await mockFavoritePickerJourneyAsync(page);
            await page.goto('/meals');
            await page.getByRole('button', { name: /Add from favorites/ }).click();
            const dialog = page.getByRole('dialog', { name: 'Add from favorites', exact: true });
            const rows = dialog.locator('fd-favorite-meal-row');
            await expect(rows).toHaveCount(FAVORITES_PAGE_SIZE);
            await rows.nth(0).getByRole('button', { name: 'Remove from favorites', exact: true }).click();
            await expect(rows.nth(0).getByRole('button', { name: 'Undo: Favorite 1' })).toBeFocused();
            await rows.nth(1).getByRole('button', { name: 'Remove from favorites', exact: true }).click();
            await expect(dialog.locator('.favorite-row__undo')).toHaveCount(2);
            await rows.nth(0).getByRole('button', { name: 'Undo: Favorite 1' }).click();
            await expect(rows.nth(0)).toContainText('Could not restore the meal');
            await rows.nth(0).getByRole('button', { name: 'Undo: Favorite 1' }).click();
            await expect(rows.nth(0).getByRole('button', { name: 'Remove from favorites', exact: true })).toBeFocused();
            await expect(dialog.locator('.favorite-row__undo')).toHaveCount(1);
            await dialog.getByRole('button', { name: '2', exact: true }).click();
            await expect(dialog.locator('.favorite-row__undo')).toHaveCount(0);
            await expect(rows).toHaveCount(1);
            await dialog.getByRole('textbox').fill('Favorite 1');
            await expect(rows).toHaveCount(FAVORITES_SEARCH_MATCHES);
            await expect(rows.first()).toContainText('Favorite 1');
            expect(await dialog.evaluate(element => element.scrollWidth > element.clientWidth)).toBe(false);
            await dialog.getByRole('button', { name: 'Close', exact: true }).click();
            await page.getByRole('button', { name: /Add from favorites/ }).click();
            await expect(dialog.locator('.favorite-row__undo')).toHaveCount(0);
            await expect(rows).toHaveCount(FAVORITES_PAGE_SIZE);
        });
    }
});

async function mockFavoritePickerJourneyAsync(page: Page): Promise<void> {
    const favorites = Array.from({ length: 12 }, (_, index) => ({
        id: `f${index + 1}`,
        mealId: `m${index + 1}`,
        name: `Favorite ${index + 1}`,
        itemNames: ['Rice', 'Chicken'],
        createdAtUtc: '2026-01-01T00:00:00Z',
        mealDate: '2026-01-01T00:00:00Z',
        mealType: 'Lunch',
        totalCalories: 500,
        totalProteins: 30,
        totalFats: 20,
        totalCarbs: 50,
        totalFiber: 5,
        itemCount: 2,
    }));
    const removed = new Set<string>();
    let failRestore = true;
    await page.route('**/api/v1/meals/overview**', async route =>
        route.fulfill({ json: { ...createMealsOverview(), favoriteTotalCount: favorites.length - removed.size } }),
    );
    await page.route('**/api/v1/favorite-meals/**', async route => {
        const url = new URL(route.request().url());
        if (url.pathname.endsWith('/page')) {
            const pageNumber = Number(url.searchParams.get('page'));
            const limit = Number(url.searchParams.get('limit'));
            const search = url.searchParams.get('search')?.toLowerCase() ?? '';
            const active = favorites.filter(item => !removed.has(item.id) && item.name.toLowerCase().includes(search));
            await route.fulfill({
                json: {
                    data: active.slice((pageNumber - 1) * limit, pageNumber * limit),
                    page: pageNumber,
                    limit,
                    totalItems: active.length,
                    totalPages: Math.ceil(active.length / limit),
                },
            });
            return;
        }
        const segments = url.pathname.split('/');
        const restoring = url.pathname.endsWith('/restore');
        const id = segments.at(restoring ? RESTORE_PATH_OFFSET : -1) ?? '';
        if (restoring) {
            if (failRestore) {
                failRestore = false;
                await route.fulfill({ status: 500, json: { message: 'Retry' } });
                return;
            }
            removed.delete(id);
            await route.fulfill({ json: favorites.find(item => item.id === id) });
        } else {
            removed.add(id);
            await route.fulfill({ status: 204 });
        }
    });
}

const MEAL_EDIT_MOBILE_WIDTH = 390;
const MEAL_EDIT_DESKTOP_WIDTH = 1280;
const MEAL_EDIT_VIEWPORTS = [MEAL_EDIT_MOBILE_WIDTH, MEAL_EDIT_DESKTOP_WIDTH] as const;

test.describe('meal editing regression', () => {
    for (const width of MEAL_EDIT_VIEWPORTS) {
        test(`failed save preserves edits and retry round-trips the meal at ${width}px`, async ({ page }) => {
            await page.setViewportSize({ width, height: 900 });
            const state = await mockEditableMealAsync(page);
            await page.goto('/meals/meal-1/edit');
            await editMealAmountAsync(page, '175.5');
            await expect(page.getByRole('textbox', { name: 'Calories, kcal', exact: true })).toHaveValue('175.5');
            await page.getByText('Photo and comment (optional)', { exact: true }).click();
            await page.getByRole('textbox', { name: 'Comment', exact: true }).fill('Lunch after training');
            const save = page.locator('fd-meal-nutrition-sidebar').getByRole('button', { name: 'Save', exact: true });
            await save.click();
            await expect(page.getByText('Temporary meal save failure', { exact: true })).toBeVisible();
            await expect(page).toHaveURL(/\/meals\/meal-1\/edit$/);
            await expect(page.getByRole('textbox', { name: 'Comment', exact: true })).toHaveValue('Lunch after training');
            await save.click();
            await expect.poll(() => state.writes.length).toBe(2);
            await expect(save).toBeDisabled();
            await expect(page.locator('fd-page-header').getByRole('button', { name: 'Save', exact: true })).toBeDisabled();
            state.releaseSave();
            await expect(page).toHaveURL(/\/meals$/);
            expect(state.writes).toHaveLength(2);
            expect(state.writes[0]).toEqual(state.writes[1]);
            expect(state.writes[1]).toMatchObject({
                comment: 'Lunch after training',
                date: '2026-04-19T18:00:00.000Z',
                isNutritionAutoCalculated: true,
                items: [{ productId: 'editable-product', amount: 175.5, origin: 'Manual' }],
            });
            await page.goto('/meals/meal-1/edit');
            await page.getByText('Photo and comment (optional)', { exact: true }).click();
            await expect(page.getByRole('textbox', { name: 'Comment', exact: true })).toHaveValue('Lunch after training');
            await page.getByRole('button', { name: /Edit manual item/ }).click();
            await expect(page.getByRole('dialog').getByRole('spinbutton', { name: 'Amount' })).toHaveValue('175.5');
            await page.getByRole('dialog').getByRole('button', { name: 'Cancel', exact: true }).click();
            await page.locator('fd-page-header').getByRole('button', { name: 'Cancel', exact: true }).click();
            await expect(page).toHaveURL(/\/meals$/);
            expect(state.writes).toHaveLength(2);
        });

        test(`cancel protects item-dialog edits and discard does not save at ${width}px`, async ({ page }) => {
            await page.setViewportSize({ width, height: 900 });
            const state = await mockEditableMealAsync(page);
            await page.goto('/meals/meal-1/edit');
            await editMealAmountAsync(page, '250');
            const cancel = page.locator('fd-page-header').getByRole('button', { name: 'Cancel', exact: true });
            await cancel.click();
            const confirmation = page.getByRole('dialog', { name: 'Unsaved changes', exact: true });
            await expect(confirmation).toContainText('Unsaved changes');
            await confirmation.getByRole('button', { name: 'Stay on page', exact: true }).click();
            await expect(page).toHaveURL(/\/meals\/meal-1\/edit$/);
            await page.getByRole('button', { name: /Edit manual item/ }).click();
            await expect(page.getByRole('dialog').getByRole('spinbutton', { name: 'Amount' })).toHaveValue('250');
            await page.getByRole('dialog').getByRole('button', { name: 'Cancel', exact: true }).click();
            await cancel.click();
            await confirmation.getByRole('button', { name: "Don't save", exact: true }).click();
            await expect(page).toHaveURL(/\/meals$/);
            expect(state.writes).toEqual([]);
            await page.goto('/meals/meal-1/edit');
            await page.getByRole('button', { name: /Edit manual item/ }).click();
            await expect(page.getByRole('dialog').getByRole('spinbutton', { name: 'Amount' })).toHaveValue('100');
        });
    }
});

async function editMealAmountAsync(page: Page, amount: string): Promise<void> {
    await page.getByRole('button', { name: /Edit manual item/ }).click();
    const dialog = page.getByRole('dialog');
    await dialog.getByRole('spinbutton', { name: 'Amount' }).fill('0');
    await expect(dialog.getByRole('button', { name: 'Save item', exact: true })).toBeDisabled();
    await dialog.getByRole('spinbutton', { name: 'Amount' }).fill(amount);
    await dialog.getByRole('button', { name: 'Save item', exact: true }).click();
    await expect(dialog).toHaveCount(0);
}

async function mockEditableMealAsync(page: Page): Promise<{ writes: unknown[]; releaseSave: () => void }> {
    await authenticateUserAsync(page);
    await mockAuthenticatedClientApiAsync(page);
    let releaseSave = (): void => {
        /* Assigned by the pending save promise below. */
    };
    const pendingSave = new Promise<void>(resolve => {
        releaseSave = resolve;
    });
    const state = { writes: [] as unknown[], releaseSave };
    let item = createMealItem('editable', 'meal-1', 'Carrots', TEST_IMAGE_URLS[0]);
    let meal = createMeal('meal-1', '2026-04-19T18:00:00Z', [item]);
    await page.route('**/api/v1/meals/meal-1', async route => {
        if (route.request().method() === 'PATCH') {
            const body = route.request().postDataJSON() as { comment: string; items: Array<{ amount: number }> };
            state.writes.push(body);
            if (state.writes.length === 1) {
                await route.fulfill({ status: 500, json: { message: 'Temporary meal save failure' } });
                return;
            }
            await pendingSave;
            item = { ...item, amount: body.items[0].amount };
            meal = { ...meal, comment: body.comment, items: [item] };
        }
        await route.fulfill({ json: meal });
    });
    return state;
}

const MEAL_DIALOG_VIEWPORTS = [MEAL_EDIT_MOBILE_WIDTH, MEAL_EDIT_DESKTOP_WIDTH];
const MEAL_DIALOG_ITEM_COUNT = 7;
const MEAL_DIALOG_PREVIEW_COUNT = 5;
const MEAL_DIALOG_MACRO_COUNT = 4;

test.describe('meal detail and gallery regression', () => {
    for (const width of MEAL_DIALOG_VIEWPORTS) {
        test(`separate photo navigation and expandable details at ${width}px`, async ({ page }, testInfo) => {
            await page.setViewportSize({ width, height: 900 });
            await authenticateUserAsync(page);
            await mockAuthenticatedClientApiAsync(page);
            const meal = createMeal(
                'meal-1',
                '2026-05-07T20:40:00Z',
                Array.from({ length: MEAL_DIALOG_ITEM_COUNT }, (_, i) =>
                    createMealItem(`item-${i}`, 'meal-1', `Ingredient ${i + 1}`, TEST_IMAGE_URLS[i % TEST_IMAGE_URLS.length]),
                ),
            );
            await page.route('**/api/v1/meals/meal-1', async route => route.fulfill({ json: meal }));
            await page.route('**/api/v1/meals/overview**', async route =>
                route.fulfill({
                    json: {
                        allMeals: { data: [meal], page: 1, limit: 20, totalPages: 1, totalItems: 1 },
                        favoriteItems: [],
                        favoriteTotalCount: 0,
                    },
                }),
            );
            await page.goto('/meals');
            const card = page.locator('fd-meal-card').first();
            await card.locator('.entity-card__thumb').press('Enter');
            const gallery = page.locator('fd-ui-image-preview-dialog');
            await expect(gallery).toBeVisible();
            await expect(page.locator('fd-meal-detail')).toHaveCount(0);
            await expect(gallery.locator('img')).toHaveCount(1);
            await gallery.getByRole('button', { name: 'Next photo' }).click();
            await expect(gallery).toContainText('2 / 4');
            await gallery.press('ArrowLeft');
            await expect(gallery).toContainText('1 / 4');
            await expect(gallery.locator('img')).toBeVisible();
            await expect
                .poll(async () => gallery.locator('img').evaluate(image => (image as HTMLImageElement).naturalWidth))
                .toBeGreaterThan(0);
            expect(await gallery.locator('.fd-ui-dialog__body').evaluate(el => el.scrollHeight <= el.clientHeight + 1)).toBe(true);
            await page.screenshot({ path: testInfo.outputPath(`gallery-${width}.png`) });
            await page.keyboard.press('Escape');
            await expect(gallery).toHaveCount(0);
            await card.locator('.entity-card__open-button').click();
            const detail = page.locator('fd-meal-detail');
            await expect(detail).toBeVisible();
            await expect(detail.locator('.meal-detail__list-row')).toHaveCount(MEAL_DIALOG_PREVIEW_COUNT);
            const expand = detail.getByRole('button', { name: /Show 2 more/ });
            await expand.click();
            await expect(detail.locator('.meal-detail__list-row')).toHaveCount(MEAL_DIALOG_ITEM_COUNT);
            await detail.getByRole('button', { name: /^Hide/ }).click();
            await expect(detail.locator('.meal-detail__list-row')).toHaveCount(MEAL_DIALOG_PREVIEW_COUNT);
            await page.screenshot({ path: testInfo.outputPath(`summary-${width}.png`) });
            await expect(detail.getByRole('tab')).toHaveCount(0);
            await expect(detail.locator('.meal-detail__macro-summary dd')).toHaveCount(MEAL_DIALOG_MACRO_COUNT);
            await expect(detail.locator('input')).toHaveCount(0);
            expect(await detail.evaluate(el => el.scrollWidth <= el.clientWidth)).toBe(true);
            await page.screenshot({ path: testInfo.outputPath(`nutrients-${width}.png`) });
            await page.keyboard.press('Escape');
            await expect(detail).toHaveCount(0);
        });
    }
});

function createRecipeRedesignFixtures(): { recipe: Record<string, unknown>; favorite: Record<string, unknown> } {
    const recipe = {
        ...createOwnedRecipe(),
        imageUrl: TEST_IMAGE_URLS[0],
        isFavorite: true,
        favoriteRecipeId: 'favorite-1',
        steps: [
            {
                id: 'step-1',
                stepNumber: 1,
                instruction: 'Mix and roast.',
                imageUrl: TEST_IMAGE_URLS[1],
                ingredients: Array.from({ length: MEAL_DIALOG_ITEM_COUNT }, (_, index) => ({
                    id: `i-${index}`,
                    amount: 100,
                    productName: `Ingredient ${index + 1}`,
                    productBaseUnit: 'G',
                })),
            },
        ],
    };
    const favorite = {
        id: 'favorite-1',
        recipeId: 'recipe-1',
        recipeName: 'Roasted vegetable bowl',
        name: null,
        createdAtUtc: '',
        imageUrl: TEST_IMAGE_URLS[0],
        servings: 2,
        totalCalories: 640,
        totalProteins: 24,
        totalFats: 18,
        totalCarbs: 92,
        totalFiber: 16,
        ingredientCount: 7,
        ingredientNames: ['Rice', 'Carrots'],
    };
    return { recipe, favorite };
}

test.describe('recipe redesign regression', () => {
    for (const width of MEAL_DIALOG_VIEWPORTS) {
        test(`recipe summary, cooking, photos and favorite undo at ${width}px`, async ({ page }, testInfo) => {
            await page.setViewportSize({ width, height: 900 });
            await authenticateUserAsync(page);
            await mockAuthenticatedClientApiAsync(page);
            const fixtures = createRecipeRedesignFixtures();
            const recipe = fixtures.recipe;
            let favorite = fixtures.favorite;
            let removed = false;
            let pageRequests = 0;
            await page.route('**/api/v1/recipes/overview**', async route =>
                route.fulfill({
                    json: {
                        recentItems: [],
                        allRecipes: { data: [recipe], page: 1, limit: 10, totalPages: 1, totalItems: 1 },
                        favoriteItems: [],
                        favoriteTotalCount: removed ? 0 : 1,
                    },
                }),
            );
            await page.route('**/api/v1/recipes/recipe-1', async route => route.fulfill({ json: recipe }));
            await page.route('**/api/v1/favorite-recipes**', async route => {
                const url = new URL(route.request().url());
                if (url.pathname.endsWith('/page')) {
                    pageRequests++;
                    await route.fulfill({
                        json: { data: removed ? [] : [favorite], page: 1, limit: 10, totalPages: 1, totalItems: removed ? 0 : 1 },
                    });
                } else if (route.request().method() === 'DELETE') {
                    removed = true;
                    await route.fulfill({ status: 204 });
                } else if (route.request().method() === 'POST') {
                    removed = false;
                    favorite = { ...favorite, id: 'favorite-restored' };
                    await route.fulfill({ json: favorite });
                } else {
                    await route.fulfill({ json: true });
                }
            });
            await page.goto('/recipes');
            const card = page.locator('fd-recipe-card').first();
            await expect(card).toContainText('Ingredient 1');
            expect(pageRequests).toBe(0);
            await card.locator('.entity-card__thumb').press('Enter');
            const gallery = page.locator('fd-ui-image-preview-dialog');
            await expect(gallery).toContainText('1 / 2');
            await gallery.press('ArrowRight');
            await expect(gallery).toContainText('2 / 2');
            await page.keyboard.press('Escape');
            await card.locator('.entity-card__open-button').click();
            const detail = page.locator('fd-recipe-detail');
            await expect(detail.getByRole('tab')).toHaveCount(2);
            await expect(detail.locator('.recipe-detail__macro-summary dd')).toHaveCount(MEAL_DIALOG_MACRO_COUNT);
            await expect(detail.locator('.recipe-detail__list-row')).toHaveCount(MEAL_DIALOG_PREVIEW_COUNT);
            await detail.getByRole('button', { name: /Show 2 more/ }).click();
            await expect(detail.locator('.recipe-detail__list-row')).toHaveCount(MEAL_DIALOG_ITEM_COUNT);
            await page.screenshot({ path: testInfo.outputPath(`recipe-summary-${width}.png`) });
            await detail.getByRole('tab').nth(1).click();
            await expect(detail).toContainText('Mix and roast.');
            await page.keyboard.press('Escape');
            await page.getByRole('button', { name: /Favorites ·/ }).click();
            const picker = page.locator('fd-recipe-favorites-picker');
            await expect(picker).toContainText('Rice, Carrots');
            const row = picker.locator('fd-favorite-recipe-row');
            await expect(row.locator('img')).toHaveAttribute('src', TEST_IMAGE_URLS[0]);
            await row.getByRole('button', { name: 'Remove from favorites', exact: true }).click();
            await expect(row.getByRole('button', { name: /Undo/ })).toBeVisible();
            await row.getByRole('button', { name: /Undo/ }).click();
            await expect(row.getByRole('button', { name: 'Remove from favorites', exact: true })).toBeVisible();
            await row.getByRole('button', { name: 'Remove from favorites', exact: true }).click();
            await expect(row.getByRole('button', { name: /Undo/ })).toBeVisible();
            await row.getByRole('button', { name: /Undo/ }).click();
            expect(pageRequests).toBe(1);
            expect(await picker.evaluate(el => el.scrollWidth <= el.clientWidth)).toBe(true);
            await page.screenshot({ path: testInfo.outputPath(`recipe-favorites-${width}.png`) });
            await page.keyboard.press('Escape');
        });
    }
});

test.describe('product redesign regression', () => {
    for (const width of MEAL_DIALOG_VIEWPORTS) {
        test(`product details and favorite undo at ${width}px`, async ({ page }, testInfo) => {
            await page.setViewportSize({ width, height: 900 });
            await authenticateUserAsync(page);
            await mockAuthenticatedClientApiAsync(page);
            const product = { ...createOwnedProduct(), isFavorite: true, favoriteProductId: 'pf1' };
            let favorite = { ...product, id: 'pf1', productId: 'p1', productName: 'Greek yogurt', name: null, preferredPortionAmount: 150 };
            let removed = false;
            let requests = 0;
            await page.route('**/api/v1/products/overview**', async route => {
                expect(new URL(route.request().url()).searchParams.get('favoriteLimit')).toBe('0');
                await route.fulfill({
                    json: {
                        recentItems: [],
                        allProducts: { data: [product], page: 1, limit: 10, totalPages: 1, totalItems: 1 },
                        favoriteItems: [],
                        favoriteTotalCount: 1,
                    },
                });
            });
            await page.route('**/api/v1/products/p1', async route => route.fulfill({ json: product }));
            await page.route('**/api/v1/favorite-products**', async route => {
                const url = new URL(route.request().url());
                if (url.pathname.endsWith('/page')) {
                    requests++;
                    expect(url.searchParams.get('limit')).toBe('10');
                    await route.fulfill({
                        json: { data: removed ? [] : [favorite], page: 1, limit: 10, totalPages: 1, totalItems: removed ? 0 : 1 },
                    });
                } else if (route.request().method() === 'DELETE') {
                    removed = true;
                    await route.fulfill({ status: 204 });
                } else if (route.request().method() === 'POST') {
                    removed = false;
                    favorite = { ...favorite, id: 'pf2' };
                    await route.fulfill({ json: favorite });
                } else {
                    await route.fulfill({ json: true });
                }
            });
            await page.goto('/products');
            const card = page.locator('fd-product-card').first();
            await expect(card).toContainText('To diary');
            expect(requests).toBe(0);
            await expect(card.locator('.entity-card__placeholder-icon')).toBeVisible();
            await page.screenshot({ path: testInfo.outputPath(`products-${width}.png`) });
            await card.locator('.entity-card__open-button').click();
            const detail = page.locator('fd-product-detail');
            await expect(detail.getByRole('tab')).toHaveCount(0);
            await expect(detail.locator('.product-detail__macro-summary dd')).toHaveCount(MEAL_DIALOG_MACRO_COUNT + 1);
            await expect(detail).toContainText('100');
            await page.screenshot({ path: testInfo.outputPath(`product-detail-${width}.png`) });
            await page.keyboard.press('Escape');
            await page.getByRole('button', { name: /Favorites ·/ }).click();
            const picker = page.locator('fd-product-favorites-picker');
            const row = picker.locator('fd-favorite-product-row');
            await expect(row).toContainText('Greek yogurt');
            await row.getByRole('button', { name: 'Remove from favorites', exact: true }).click();
            await expect(row.getByRole('button', { name: /Undo/ })).toBeVisible();
            await row.getByRole('button', { name: /Undo/ }).click();
            await expect(row.getByRole('button', { name: 'Remove from favorites', exact: true })).toBeVisible();
            expect(requests).toBe(1);
            expect(await picker.evaluate(el => el.scrollWidth <= el.clientWidth)).toBe(true);
            await page.screenshot({ path: testInfo.outputPath(`product-favorites-${width}.png`) });
        });
    }
});

test.describe('product form visual regression', () => {
    for (const width of MEAL_DIALOG_VIEWPORTS) {
        test(`nutrition entry at ${width}px`, async ({ page }, testInfo) => {
            await page.setViewportSize({ width, height: 900 });
            await authenticateUserAsync(page);
            await mockAuthenticatedClientApiAsync(page);
            const errors: string[] = [];
            page.on('pageerror', error => errors.push(error.message));
            await page.goto('/products/add');
            const form = page.locator('fd-product-manage-form');
            await expect(form).toBeVisible();
            await expect(form.locator('fd-nutrition-editor fd-ui-input')).toHaveCount(PRODUCT_NUTRIENT_FIELD_COUNT);
            await expect(form.locator('.nutrition-editor__macro-bar')).toBeHidden();
            await expect(form.locator('.image-upload-field__hint')).toBeHidden();
            const additional = form.locator('details.product-manage__additional');
            await expect(additional).not.toHaveAttribute('open');
            await expect(additional.locator('summary')).toContainText('Only me');
            await additional.locator('summary').click();
            await expect(additional.locator('textarea')).toBeVisible();
            await additional.locator('summary').click();
            await expect(form.locator('fd-page-header fd-ui-button[icon="save"]')).toHaveCount(0);
            const description = form.locator('fd-ui-textarea textarea').first();
            const initialHeight = await description.evaluate(element => element.clientHeight);
            const twoLineHeight = await description.evaluate(
                element => Math.ceil(Number.parseFloat(getComputedStyle(element).lineHeight)) * 2,
            );
            expect(initialHeight).toBeLessThanOrEqual(twoLineHeight + 1);
            await description.fill(Array.from({ length: 12 }, () => 'Product description').join('\n'));
            await expect.poll(async () => description.evaluate(element => element.clientHeight)).toBeGreaterThan(initialHeight);
            await description.fill('');
            await expect.poll(async () => description.evaluate(element => element.clientHeight)).toBe(initialHeight);
            const calories = form.locator('.nutrition-editor__input--calories input');
            await expect(calories).toHaveAttribute('placeholder', '0');
            await expect(calories).toHaveValue('');
            await calories.fill('0');
            await calories.blur();
            await form.locator('.nutrition-editor__input--proteins input').fill('10');
            await expect(form.locator('.nutrition-editor__macro-bar')).toBeVisible();
            await page.screenshot({ path: testInfo.outputPath(`product-form-${width}.png`), fullPage: true });
            expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true);
            expect(errors).toEqual([]);
        });
    }
});

test.describe('product creation behavior', () => {
    test('saves comma decimals in portion mode as normalized base values', async ({ page }) => {
        await authenticateUserAsync(page);
        await mockAuthenticatedClientApiAsync(page);
        const payloads: Array<Record<string, unknown>> = [];
        await page.route(/\/api\/v1\/products\/?(?:\?|$)/u, async route => {
            if (route.request().method() !== 'POST') {
                await route.fallback();
                return;
            }
            const payload = route.request().postDataJSON() as Record<string, unknown>;
            payloads.push(payload);
            await route.fulfill({ json: { ...createOwnedProduct(), ...payload } });
        });
        await page.goto('/products/add');
        await page.getByRole('combobox', { name: 'Name *', exact: true }).fill('QA decimal product');
        await page.keyboard.press('Escape');
        const calories = page.locator('.nutrition-editor__input--calories input');
        const protein = page.locator('.nutrition-editor__input--proteins input');
        await calories.fill('120');
        await protein.pressSequentially('10,5');
        await expect(protein).toHaveValue('10,5');
        await page.getByRole('textbox', { name: 'Default portion', exact: true }).fill('250');
        await page.getByRole('radio', { name: 'Per serving', exact: true }).click();
        await expect(calories).toHaveValue('300');
        await protein.fill('25,5');
        await page.locator('details summary').click();
        await page.getByRole('textbox', { name: 'Comment', exact: true }).fill('Keep this comment');
        await page.getByRole('button', { name: 'Create product', exact: true }).click();
        await expect(page).toHaveURL(/\/products$/u);
        expect(payloads).toHaveLength(1);
        expect(payloads[0]).toMatchObject({
            baseAmount: 100,
            defaultPortionAmount: 250,
            caloriesPerBase: 120,
            proteinsPerBase: 10.2,
            comment: 'Keep this comment',
            visibility: 'Private',
        });
    });
});

test.describe('product creation behavior', () => {
    test('blocks invalid values, saves zero nutrition and retries after a server error', async ({ page }) => {
        await authenticateUserAsync(page);
        await mockAuthenticatedClientApiAsync(page);
        let attempts = 0;
        await page.route(/\/api\/v1\/products\/?(?:\?|$)/u, async route => {
            if (route.request().method() !== 'POST') {
                await route.fallback();
                return;
            }
            attempts += 1;
            if (attempts === 1) {
                await route.fulfill({ status: 500, json: { message: 'Test failure' } });
                return;
            }
            await route.fulfill({ json: { ...createOwnedProduct(), ...(route.request().postDataJSON() as Record<string, unknown>) } });
        });
        await page.goto('/products/add');
        const submit = page.getByRole('button', { name: 'Create product', exact: true });
        await expect(submit).toBeDisabled();
        await page.getByRole('combobox', { name: 'Name *', exact: true }).fill('QA water');
        await page.keyboard.press('Escape');
        const calories = page.locator('.nutrition-editor__input--calories input');
        const protein = page.locator('.nutrition-editor__input--proteins input');
        await calories.fill('0');
        await protein.fill('1,2,3');
        await protein.blur();
        await expect(submit).toBeDisabled();
        await protein.fill('-1');
        await expect(submit).toBeDisabled();
        await protein.fill('101');
        await expect(submit).toBeDisabled();
        await protein.fill('');
        await calories.fill('-1');
        await expect(submit).toBeDisabled();
        await calories.fill('0');
        await expect(submit).toBeEnabled();
        await submit.click();
        await expect(page.locator('.product-manage__footer-error')).toBeVisible();
        await expect(page.getByRole('combobox', { name: 'Name *', exact: true })).toHaveValue('QA water');
        await expect(calories).toHaveValue('0');
        await submit.click();
        await expect(page).toHaveURL(/\/products$/u);
        expect(attempts).toBe(2);
    });
});
