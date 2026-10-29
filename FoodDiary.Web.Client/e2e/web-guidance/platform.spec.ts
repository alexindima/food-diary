import { expect, test } from '@playwright/test';

import { inspectPlatformPage } from '../../quality/modern-web-guidance/browser-platform.mjs';

test('public dialog entrance respects live reduced motion without test CSS overrides', async ({ page }) => {
    await page.emulateMedia({ reducedMotion: 'no-preference' });
    await page.goto('/');
    await page.locator('html[data-i18n-ready] main h1').waitFor();
    await page.getByRole('button', { name: 'Sign in', exact: true }).first().click();
    const dialog = page.locator('[role="dialog"] .fd-ui-dialog');
    await expect(dialog).toBeVisible();
    await expect.poll(async () => dialog.evaluate(el => getComputedStyle(el).animationName)).not.toBe('none');

    await page.emulateMedia({ reducedMotion: 'reduce' });
    await expect.poll(async () => dialog.evaluate(el => getComputedStyle(el).animationName)).toBe('none');
    await expect(dialog).toHaveCSS('opacity', '1');
    await expect(dialog).toHaveCSS('transform', 'none');

    await page.keyboard.press('Escape');
    await expect(dialog).toHaveCount(0);
    await page.getByRole('button', { name: 'Sign in', exact: true }).first().click();
    await expect(dialog).toBeVisible();
    await expect(dialog).toHaveCSS('animation-name', 'none');
    await expect(dialog).toHaveCSS('opacity', '1');
});

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
