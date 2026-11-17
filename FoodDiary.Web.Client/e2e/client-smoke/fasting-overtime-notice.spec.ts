import { expect, test } from '@playwright/test';

import { userFixture } from '../../src/testing/api-fixtures';
import { installClientAuthApiAsync } from '../support/auth-flow-fixtures';
import { fixtureUuid, jsonFixture, OpenApiFixtures } from '../support/openapi-fixtures.mjs';

const contract = new OpenApiFixtures();
const PLANNED_DURATION_HOURS = 16;
const MIN_NOTICE_GAP_PX = 8;

test('separates the overtime fasting notice from the actions', async ({ page }, testInfo) => {
    await page.clock.setFixedTime(new Date('2026-11-17T12:00:00Z'));
    await page.addInitScript(() => {
        localStorage.setItem('fd_language', 'ru');
    });
    const auth = await installClientAuthApiAsync(page, userFixture({ language: 'ru' }));
    auth.authenticated = true;
    await page.route('**/api/v1/fasting/overview*', async route => {
        await route.fulfill(
            jsonFixture(contract, 'GET', route.request().url(), {
                currentSession: {
                    id: fixtureUuid('fasting-overtime-notice'),
                    startedAtUtc: '2026-11-16T18:00:00Z',
                    endedAtUtc: null,
                    initialPlannedDurationHours: PLANNED_DURATION_HOURS,
                    addedDurationHours: 0,
                    plannedDurationHours: PLANNED_DURATION_HOURS,
                    protocol: 'Fast16Eat8',
                    planType: 'Intermittent',
                    occurrenceKind: 'FastingWindow',
                    isCompleted: false,
                    status: 'Active',
                    symptoms: [],
                    checkIns: [],
                },
                stats: {
                    totalCompleted: 0,
                    currentStreak: 0,
                    averageDurationHours: 0,
                    completionRateLast30Days: 0,
                    checkInRateLast30Days: 0,
                },
                insights: { alerts: [], insights: [] },
                history: { data: [], page: 1, limit: 10, totalPages: 0, totalItems: 0 },
            }),
        );
    });
    await page.goto('/fasting');

    const notice = page.getByRole('status').filter({ hasText: 'Плановое время истекло' });
    const end = page.getByRole('button', { name: 'Завершить голодание', exact: true });
    const checkIn = page.getByRole('button', { name: 'Отметить самочувствие', exact: true });
    for (const viewport of [
        { width: 1440, height: 900 },
        { width: 390, height: 844 },
    ]) {
        await page.setViewportSize(viewport);
        await expect(notice).toBeVisible();
        await expect(end).toBeVisible();
        await expect(checkIn).toBeVisible();
        const noticeBounds = await notice.boundingBox();
        const actionsBounds = await end.boundingBox();
        expect(noticeBounds).not.toBeNull();
        expect(actionsBounds).not.toBeNull();
        if (noticeBounds !== null && actionsBounds !== null) {
            const gap = actionsBounds.y - noticeBounds.y - noticeBounds.height;
            expect(gap, 'The overtime notice touches the fasting actions').toBeGreaterThanOrEqual(MIN_NOTICE_GAP_PX);
        }
        await notice.locator('..').screenshot({ path: testInfo.outputPath(`fasting-overtime-${viewport.width}.png`) });
    }
});
