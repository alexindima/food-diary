import { expect, test } from '@playwright/test';

import { userFixture } from '../../src/testing/api-fixtures';
import { installClientAuthApiAsync } from '../support/auth-flow-fixtures';

test('shows complete account menu labels with collapsed desktop navigation', async ({ page }, testInfo) => {
    await page.setViewportSize({ width: 1440, height: 900 });
    await page.addInitScript(() => {
        localStorage.setItem('fd_language', 'ru');
    });
    const session = await installClientAuthApiAsync(page, userFixture({ language: 'ru', surfaceStyle: 'glass' }));
    session.authenticated = true;
    await page.goto('/dashboard');
    await expect(page.locator('fd-dashboard')).toBeVisible();
    await expect(page.locator('html')).toHaveAttribute('data-surface-style', 'glass');

    await page.getByRole('button', { name: 'Свернуть навигацию' }).click();
    const trigger = page.getByRole('button', { name: 'Меню пользователя' });
    await trigger.click();
    const menu = page.getByRole('menu', { name: 'Меню пользователя' });
    for (const name of ['Профиль', 'Выход']) {
        const item = menu.getByRole('menuitem', { name, exact: true });
        const label = item.getByText(name, { exact: true });
        await expect(label).toBeVisible();
        const labelFits = await label.evaluate(element => {
            const text = document.createRange();
            text.selectNodeContents(element);
            const textRight = text.getBoundingClientRect().right;
            const itemRight = element.closest('[role="menuitem"]')?.getBoundingClientRect().right ?? 0;
            const menuRight = element.closest('[role="menu"]')?.getBoundingClientRect().right ?? 0;
            return textRight <= Math.min(itemRight, menuRight);
        });
        expect(labelFits, `Account menu label is clipped: ${name}`).toBe(true);
    }
    await menu.screenshot({ path: testInfo.outputPath('collapsed-account-menu.png') });

    await page.keyboard.press('Escape');
    await expect(menu).toBeHidden();
    await expect(trigger).toBeFocused();
});
