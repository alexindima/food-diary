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

for (const path of ['/calorie-counter', '/ru/calorie-counter']) {
    test(`SEO FAQ and related navigation remain usable at ${path}`, async ({ page }) => {
        const errors: string[] = [];
        page.on('pageerror', error => errors.push(error.message));
        await page.setViewportSize({ width: 390, height: VIEWPORT_HEIGHT });
        await page.goto(path);
        const title = page.locator('html[data-app-ready] .seo-landing__title');
        await expect(title).toBeVisible();
        const originalTitle = await title.textContent();

        const question = page.locator('.seo-landing__faq-question').first();
        await question.scrollIntoViewIfNeeded();
        await question.press('Enter');
        await expect(page.locator('.seo-landing__faq-item[open]')).toHaveCount(1);

        const related = page.locator('.seo-landing__related-card').first();
        await related.scrollIntoViewIfNeeded();
        const href = await related.getAttribute('href');
        expect(href).toMatch(path.startsWith('/ru/') ? /^\/ru\//u : /^\/(?!ru\/)/u);
        const target = new URL(href ?? '', page.url()).href;
        await page.evaluate(() => {
            document.documentElement.dataset['guidanceNavigationProbe'] = 'preserved';
        });
        await related.press('Enter');

        await expect(page).toHaveURL(target);
        await expect(title).toBeVisible();
        await expect(title).not.toHaveText(originalTitle ?? '');
        await expect(page.locator('html')).toHaveAttribute('data-guidance-navigation-probe', 'preserved');
        expect(errors).toEqual([]);
    });
}
