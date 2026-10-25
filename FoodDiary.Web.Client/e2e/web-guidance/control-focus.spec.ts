import { expect, test } from '@playwright/test';

import { inspectPlatformPage } from '../../quality/modern-web-guidance/browser-platform.mjs';

test('credential focus remains visible with system colors', async ({ page }) => {
    await page.emulateMedia({ forcedColors: 'active' });
    await page.goto('/');
    await page.locator('html[data-app-ready] main h1').waitFor();
    await page.getByRole('button', { name: 'Sign in', exact: true }).first().click();
    await expect(page.getByRole('dialog')).toBeVisible();
    const result = await inspectPlatformPage(page, 'browser:focus-visible', { openDialog: 'Sign in' });
    expect(result.findings).toEqual([]);
});
