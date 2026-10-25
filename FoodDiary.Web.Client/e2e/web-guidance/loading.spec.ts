import { expect, test } from '@playwright/test';

const INITIAL_LOAD_OBSERVATION_MS = 6500;
const VIEWPORT_HEIGHT = 844;
for (const path of ['/', '/ru']) {
    test(`offscreen preview waits for the reader before rendering at ${path}`, async ({ page }) => {
        await page.setViewportSize({ width: 390, height: VIEWPORT_HEIGHT });
        await page.goto(path);
        await page.locator('html[data-app-ready] main h1').waitFor();
        await expect(page.locator('fd-tour-host, fd-app-update-notice')).toHaveCount(0);
        const placeholder = page.locator('.landing-main__preview-placeholder');
        const rect = await placeholder.boundingBox();
        expect(rect?.y).toBeGreaterThan(VIEWPORT_HEIGHT);
        // This observation window deliberately includes the former five-second
        // activation timer; it is part of the loading behavior being measured.
        await page.waitForTimeout(INITIAL_LOAD_OBSERVATION_MS);
        expect(await page.locator('fd-landing-preview-tour').count(), 'Offscreen preview must not instantiate during initial load').toBe(0);
        await placeholder.scrollIntoViewIfNeeded();
        await expect(page.locator('fd-landing-preview-tour')).toBeVisible();
    });
}
