import type { Page, WebSocketRoute } from '@playwright/test';

import type { UserHttpResponse } from '../../src/app/shared/api/sdk/generated/model/user-http-response';
import { userFixture } from '../../src/testing/api-fixtures';
import { jsonFixture, OpenApiFixtures } from './openapi-fixtures.mjs';
import { syntheticAuthToken } from './synthetic-auth-token';

const RECORD_SEPARATOR = '\u001E';
const HTTP_NO_CONTENT = 204;
const contract = new OpenApiFixtures();

export async function seedClientSessionAsync(page: Page, user: UserHttpResponse): Promise<void> {
    if (user.id === undefined) {
        throw new Error('The synthetic session requires an owning user ID.');
    }
    await page.addInitScript(
        state => {
            if (sessionStorage.getItem('fooddiary.auth-flow-test-seeded') === 'true') {
                return;
            }
            sessionStorage.setItem('fooddiary.auth-flow-test-seeded', 'true');
            sessionStorage.setItem('authToken', state.token);
            sessionStorage.setItem('userId', state.userId);
            sessionStorage.setItem('emailConfirmed', String(state.confirmed));
            sessionStorage.setItem('mustChangePassword', 'false');
        },
        { token: syntheticAuthToken(user.id), userId: user.id, confirmed: user.isEmailConfirmed === true },
    );
}

export async function installClientAuthApiAsync(
    page: Page,
    user = userFixture(),
): Promise<{
    user: UserHttpResponse;
    profileRequests: number;
    unexpectedRequests: string[];
}> {
    const state = { user, profileRequests: 0, unexpectedRequests: [] as string[] };
    await page.route('**/hubs/**', async route => {
        await route.abort('blockedbyclient');
    });
    await installNotificationHubAsync(page);
    await page.route('**/api/v1/**', async route => {
        const { pathname } = new URL(route.request().url());
        const method = route.request().method();
        if (method === 'POST' && ['/api/v1/marketing/attribution-events', '/api/v1/auth/logout'].includes(pathname)) {
            contract.assertResponse(method, pathname, HTTP_NO_CONTENT, undefined);
            await route.fulfill({ status: HTTP_NO_CONTENT });
            return;
        }
        const responses: Partial<Record<string, () => unknown>> = {
            '/api/v1/auth/telegram/configuration': () => ({ loginEnabled: true, registrationEnabled: true, oidcEnabled: true }),
            '/api/v1/users/info': () => {
                state.profileRequests++;
                return state.user;
            },
            '/api/v1/users/overview': () => ({
                user: state.user,
                notificationPreferences: {
                    pushNotificationsEnabled: false,
                    fastingPushNotificationsEnabled: false,
                    socialPushNotificationsEnabled: false,
                    fastingCheckInReminderHours: 4,
                    fastingCheckInFollowUpReminderHours: 2,
                },
                webPushSubscriptions: [],
                dietologistRelationship: null,
            }),
            '/api/v1/notifications/unread-count': () => ({ count: 0 }),
            '/api/v1/notifications': () => [],
            '/api/v1/dashboard': dashboardFixture,
        };
        const response = responses[pathname];
        if (method !== 'GET' || response === undefined) {
            state.unexpectedRequests.push(`${method} ${pathname}`);
            await route.abort('blockedbyclient');
            return;
        }
        await route.fulfill(jsonFixture(contract, method, pathname, response()));
    });
    return state;
}

export async function installVerificationHubAsync(page: Page): Promise<{
    connections: number;
    handshakes: number;
    closes: number;
    verify: () => void;
}> {
    let connection: WebSocketRoute | undefined;
    const state = {
        connections: 0,
        handshakes: 0,
        closes: 0,
        verify: (): void => {
            if (connection === undefined || state.handshakes === 0) {
                throw new Error('Wait for the real SignalR client handshake before sending verification.');
            }
            connection.send(`${JSON.stringify({ type: 1, target: 'EmailVerified', arguments: [] })}${RECORD_SEPARATOR}`);
        },
    };
    await installHubNegotiationAsync(page, 'email-verification');
    await page.routeWebSocket(/\/hubs\/email-verification(?:\?|$)/u, socket => {
        state.connections++;
        connection = socket;
        let closing = false;
        socket.onMessage(message => {
            if (message.toString().includes('"protocol":"json"')) {
                state.handshakes++;
                socket.send(`{}${RECORD_SEPARATOR}`);
            }
        });
        socket.onClose(async () => {
            // Playwright echoes the close acknowledgement through onClose. Count each closed socket once.
            if (closing) {
                return;
            }
            closing = true;
            await socket.close();
            state.closes++;
        });
    });
    return state;
}

async function installNotificationHubAsync(page: Page): Promise<void> {
    await installHubNegotiationAsync(page, 'notifications');
    await page.routeWebSocket(/\/hubs\/notifications(?:\?|$)/u, socket => {
        socket.onMessage(message => {
            if (message.toString().includes('"protocol":"json"')) {
                socket.send(`{}${RECORD_SEPARATOR}`);
            }
        });
    });
}

async function installHubNegotiationAsync(page: Page, hub: 'email-verification' | 'notifications'): Promise<void> {
    await page.route(`**/hubs/${hub}/negotiate?*`, async route => {
        await route.fulfill({
            json: {
                negotiateVersion: 1,
                connectionId: `synthetic-${hub}-connection`,
                connectionToken: `synthetic-${hub}-connection`,
                availableTransports: [{ transport: 'WebSockets', transferFormats: ['Text'] }],
            },
        });
    });
}

function dashboardFixture(): Record<string, unknown> {
    return {
        date: '2026-04-19T00:00:00Z',
        dateTo: '2026-04-19T00:00:00Z',
        dailyGoal: 1900,
        weeklyCalorieGoal: 13300,
        statistics: {
            totalCalories: 0,
            averageProteins: 0,
            averageFats: 0,
            averageCarbs: 0,
            averageFiber: 0,
            proteinGoal: 120,
            fatGoal: 60,
            carbGoal: 180,
            fiberGoal: 30,
        },
        weeklyCalories: [],
        weight: { latest: null, previous: null, desiredWeightKg: null },
        waist: { latest: null, previous: null, desiredWaistCm: null },
        meals: { items: [], total: 0 },
        hydration: { dateUtc: '2026-04-19T00:00:00Z', goalMl: 2200, totalMl: 0 },
        advice: null,
        currentFastingSession: null,
        weightTrend: [],
        waistTrend: [],
        dashboardLayout: null,
        caloriesBurned: 0,
    };
}
