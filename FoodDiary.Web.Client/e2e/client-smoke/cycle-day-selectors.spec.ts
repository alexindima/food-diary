import { expect, test } from '@playwright/test';

import { FIXTURE_USER_ID, userFixture } from '../../src/testing/api-fixtures';
import { installClientAuthApiAsync } from '../support/auth-flow-fixtures';
import { fixtureUuid, jsonFixture, OpenApiFixtures } from '../support/openapi-fixtures.mjs';

const contract = new OpenApiFixtures();

test('cycle symptom severity uses the same controls as bleeding type', async ({ page }, testInfo) => {
    await page.setViewportSize({ width: 1440, height: 900 });
    await page.addInitScript(() => {
        localStorage.setItem('fd_language', 'ru');
    });
    const session = await installClientAuthApiAsync(page, userFixture({ language: 'ru' }));
    session.authenticated = true;
    await page.route('**/api/v1/cycles/current', async route => {
        await route.fulfill(
            jsonFixture(contract, 'GET', route.request().url(), {
                id: fixtureUuid('cycle-selector-check'),
                userId: FIXTURE_USER_ID,
                mode: 0,
                confidence: 0,
                trackingStartDate: '2026-01-01T00:00:00Z',
                averageCycleLength: 28,
                averagePeriodLength: 5,
                lutealLength: 14,
                isRegular: true,
                isOnboardingComplete: true,
                showFertilityEstimates: false,
                discreetNotifications: false,
                goal: 0,
                reproductiveState: 0,
                hideFromDashboard: false,
                bleedingEntries: [],
                symptoms: [],
                factors: [],
                fertilitySignals: [],
            }),
        );
    });
    await page.route('**/api/v1/cycles/current/nutrition-summary?*', async route => route.fulfill({ status: 204 }));
    await page.goto('/cycle-tracking');
    await page.getByRole('button', { name: 'Отметить сегодняшний день' }).click();
    const drawer = page.getByRole('dialog', { name: 'Ваш день' });
    await drawer.getByRole('button', { name: 'Изменения аппетита', exact: true }).click();
    const severity = drawer.getByText('Умеренная', { exact: true }).last();
    const bleeding = drawer.getByRole('group', { name: 'Тип кровотечения' }).getByRole('button').first();
    await expect(severity).toBeVisible();
    const metrics = (element: Element): { height: string; padding: string } => {
        const style = getComputedStyle(element);
        return { height: style.minHeight, padding: style.padding };
    };
    expect(await severity.evaluate(metrics), 'Appetite severity spacing differs from bleeding type').toEqual(
        await bleeding.evaluate(metrics),
    );
    await severity.click();
    await expect(severity).toHaveAttribute('aria-pressed', 'true');
    await drawer.screenshot({ path: testInfo.outputPath('cycle-day-selectors.png') });
});
