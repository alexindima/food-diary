import { expect, test } from '@playwright/test';

import { inspectPage } from '../../quality/modern-web-guidance/browser.mjs';

for (const path of ['/calorie-counter', '/ru/calorie-counter']) {
    test(`SEO contrast respects forced system colors at ${path}`, async ({ page }, testInfo) => {
        await page.setViewportSize({ width: 390, height: 844 });
        await page.emulateMedia({ forcedColors: 'active' });
        await page.goto(path);
        await page.locator('html[data-app-ready] main h1').waitFor();
        const result = await inspectPage(page, 'browser:axe', {});
        const colors = await page
            .locator('.seo-landing__hero-panel, .seo-landing__panel-title, .seo-landing__panel-text')
            .evaluateAll(elements =>
                elements.map(element => {
                    const style = getComputedStyle(element);
                    return {
                        selector: element.getAttribute('class'),
                        color: style.color,
                        background: style.backgroundColor,
                        image: style.backgroundImage,
                    };
                }),
            );
        await testInfo.attach('contrast-diagnostics', {
            body: JSON.stringify({ colors, result }, null, 2),
            contentType: 'application/json',
        });
        expect(result.findings).toEqual([]);
        expect(await page.evaluate(() => matchMedia('(forced-colors: active)').matches)).toBe(true);
        expect(colors.every(item => item.color === 'rgb(0, 0, 0)')).toBe(true);
        expect(colors[0].image).toBe('none');
    });
}
