import { expect, type Page, test } from '@playwright/test';

import { userFixture } from '../../src/testing/api-fixtures';
import { installClientAuthApiAsync } from '../support/auth-flow-fixtures';

const DESKTOP_WIDTH = 1440;
const MOBILE_WIDTH = 767;
const MOBILE_BOUNDARY = 768;
const FIRST_DESKTOP_WIDTH = 769;
const VIEWPORT_HEIGHT = 900;

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

test('closes the desktop account menu when keyboard focus moves to the page', async ({ page }) => {
    await openSidebarAtWidthAsync(page, DESKTOP_WIDTH);
    const trigger = page.locator('fd-sidebar-desktop .sidebar__user');
    const menu = page.getByRole('menu', { name: 'User menu' });
    const profile = menu.getByRole('menuitem', { name: 'Profile', exact: true });
    const logout = menu.getByRole('menuitem', { name: 'Logout', exact: true });
    const collapse = page.getByRole('button', { name: 'Collapse navigation' });
    const viewport = page.getByRole('region', { name: 'Skip to main content' });

    await trigger.press('Enter');
    await expect(profile).toBeFocused();
    await page.keyboard.press('Tab');
    await expect(logout).toBeFocused();
    await page.keyboard.press('Tab');
    await expect(collapse).toBeFocused();

    await page.keyboard.press('Enter');
    const expand = page.getByRole('button', { name: 'Expand navigation' });
    await expect(expand).toBeFocused();
    await page.keyboard.press('Enter');
    await expect(collapse).toBeFocused();

    await page.keyboard.press('Tab');
    await expect(viewport).toBeFocused();

    await expect.soft(menu).toBeHidden();
    await page.keyboard.press('Escape');
    await expect(viewport).toBeFocused();
});

test.describe('sidebar viewport transitions', () => {
    test('dismisses the desktop account menu at the mobile breakpoint', async ({ page }) => {
        await openSidebarAtWidthAsync(page, DESKTOP_WIDTH);
        const trigger = page.locator('fd-sidebar-desktop .sidebar__user');
        const menu = page.locator('fd-sidebar-desktop [role="menu"]');
        await trigger.click();
        await expect(menu).toBeVisible();

        await page.setViewportSize({ width: MOBILE_BOUNDARY, height: VIEWPORT_HEIGHT });
        await expect(page.locator('fd-sidebar-mobile nav')).toBeVisible();
        await expect(menu).toHaveCount(0);

        await page.setViewportSize({ width: DESKTOP_WIDTH, height: VIEWPORT_HEIGHT });
        await expect(trigger).toHaveAttribute('aria-expanded', 'false');
    });

    test('keeps an open mobile sheet at the last mobile width and dismisses it on desktop', async ({ page }) => {
        await openSidebarAtWidthAsync(page, MOBILE_WIDTH);
        const food = page.locator('button[data-mobile-sheet="food"]');
        const sheet = page.locator('.sidebar-mobile__sheet');
        await food.click();
        await expect(sheet).toBeVisible();

        await page.setViewportSize({ width: MOBILE_BOUNDARY, height: VIEWPORT_HEIGHT });
        await page.evaluate(async () => {
            await new Promise<void>(resolve => {
                requestAnimationFrame(() => {
                    requestAnimationFrame(() => {
                        resolve();
                    });
                });
            });
        });
        await expect(food).toHaveAttribute('aria-expanded', 'true');
        await expect(sheet).toBeVisible();

        await page.setViewportSize({ width: FIRST_DESKTOP_WIDTH, height: VIEWPORT_HEIGHT });
        await expect(sheet).toHaveCount(0);
        await expect(page.locator('fd-sidebar-desktop .sidebar__user')).toBeVisible();
    });
});

async function openSidebarAtWidthAsync(page: Page, width: number): Promise<void> {
    await page.setViewportSize({ width, height: VIEWPORT_HEIGHT });
    const session = await installClientAuthApiAsync(page, userFixture());
    session.authenticated = true;
    await page.goto('/dashboard');
    await expect(page.locator('fd-dashboard')).toBeVisible();
}
