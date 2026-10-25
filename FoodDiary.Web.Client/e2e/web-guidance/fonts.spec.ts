import { expect, test } from '@playwright/test';
const MINIMUM_TITLE_WIDTH = 100;

for (const path of ['/', '/ru']) {
    test(`public content and actions remain usable when fonts fail at ${path}`, async ({ page }) => {
        let blocked = 0;
        await page.route('**/*', async route => {
            const request = route.request();
            if (request.resourceType() === 'font' || new URL(request.url()).hostname === 'fonts.googleapis.com') {
                blocked++;
                await route.abort();
            } else {
                await route.continue();
            }
        });
        await page.setViewportSize({ width: 390, height: 844 });
        await page.goto(path);
        const title = page.locator('html[data-app-ready] main h1');
        await expect(title).toBeVisible();
        await page.evaluate(async () => document.fonts.ready);
        expect(blocked).toBeGreaterThan(0);
        expect(await title.evaluate(element => element.getBoundingClientRect().width)).toBeGreaterThan(MINIMUM_TITLE_WIDTH);
        await page
            .getByRole('button', { name: path === '/' ? 'Sign in' : 'Войти', exact: true })
            .first()
            .click();
        await expect(page.getByRole('dialog')).toBeVisible();
        await expect(page.locator('input[autocomplete="current-password"]')).toBeVisible();
    });
}
