import { expect, test } from '@playwright/test';

import { inspectPlatformPage } from '../../quality/modern-web-guidance/browser-platform.mjs';

for (const path of ['/calorie-counter', '/ru/calorie-counter']) {
    test(`SEO text enlargement preserves reflow at ${path}`, async ({ page }) => {
        await page.setViewportSize({ width: 390, height: 844 });
        await page.goto(path);
        await page.locator('html[data-app-ready] main h1').waitFor();
        const result = await inspectPlatformPage(page, 'browser:text-resize', { kind: 'public' });
        expect(result.findings, 'Enlarged text must stay inside the reading viewport').toEqual([]);
    });
}
