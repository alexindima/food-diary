import { expect, test } from '@playwright/test';

import { inspectPlatformPage } from '../../quality/modern-web-guidance/browser-platform.mjs';

for (const forcedColors of ['none', 'active'] as const) {
    test(`public keyboard actions keep visible focus with forced colors ${forcedColors}`, async ({ page }) => {
        await page.setViewportSize({ width: 390, height: 844 });
        await page.emulateMedia({ forcedColors });
        await page.goto('/');
        await page.locator('html[data-i18n-ready] main h1').waitFor();

        const result = await inspectPlatformPage(page, 'browser:focus-visible', { kind: 'public' });
        expect(result.observations).toBeGreaterThan(0);
        expect(result.findings, 'Every primary keyboard action needs a visible focus indicator').toEqual([]);
    });
}
