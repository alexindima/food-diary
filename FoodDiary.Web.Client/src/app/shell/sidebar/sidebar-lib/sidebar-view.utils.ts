import { PERCENT_MULTIPLIER } from '../../../shared/lib/nutrition.constants';
import type { MobileSheetId, SidebarActionItem, SidebarNavItem, SidebarRouteItem } from './sidebar.models';

export function buildPrimarySidebarNavItems(isDietologist: boolean, isAdmin: boolean): SidebarNavItem[] {
    const items: SidebarNavItem[] = [{ id: 'dashboard', icon: 'dashboard', labelKey: 'SIDEBAR.DASHBOARD', route: '/', exact: true }];

    if (isDietologist) {
        items.push({ id: 'dietologist', icon: 'medical_services', labelKey: 'SIDEBAR.MY_CLIENTS', route: '/dietologist' });
    }

    if (isAdmin) {
        items.push({ id: 'admin', icon: 'admin_panel_settings', labelKey: 'SIDEBAR.ADMIN_PANEL', action: 'openAdminPanel' });
    }

    return items;
}

export function isSidebarRouteItem(item: SidebarNavItem): item is SidebarRouteItem {
    return 'route' in item;
}

export function isSidebarActionItem(item: SidebarNavItem): item is SidebarActionItem {
    return 'action' in item;
}

export function normalizeSidebarPath(url: string): string {
    const path = url.split('?')[0].split('#')[0];
    return path.length > 0 ? path : '/';
}

export function isSidebarRouteActive(currentUrl: string, route: string, exact: boolean): boolean {
    const currentPath = normalizeSidebarPath(currentUrl);
    if (exact) {
        return currentPath === route;
    }

    return currentPath === route || currentPath.startsWith(`${route}/`);
}

export function calculateSidebarDailyProgressPercent(consumed: number, goal: number): number {
    if (goal <= 0) {
        return 0;
    }

    return Math.max(0, Math.min((consumed / goal) * PERCENT_MULTIPLIER, PERCENT_MULTIPLIER));
}

export function getSidebarMobileSheetLabelKey(sheet: MobileSheetId): string {
    switch (sheet) {
        case 'food':
            return 'SIDEBAR.FOOD_TRACKING';
        case 'body':
            return 'SIDEBAR.BODY_TRACKING';
        case 'reports':
            return 'SIDEBAR.REPORTS_AND_GOALS';
        case 'user':
            return 'SIDEBAR.USER_MENU';
        case null:
            return '';
    }
}

export function getSidebarMobileSheetRouteItems(
    sheet: MobileSheetId,
    foodItems: SidebarRouteItem[],
    bodyItems: SidebarRouteItem[],
    reportItems: SidebarRouteItem[],
): SidebarRouteItem[] {
    switch (sheet) {
        case 'food':
            return foodItems;
        case 'body':
            return bodyItems;
        case 'reports':
            return reportItems;
        case 'user':
        case null:
            return [];
    }
}

export function focusFirstSidebarInteractiveElement(container?: HTMLElement | null): void {
    if (container === null || container === undefined) {
        return;
    }

    const firstInteractive = container.querySelector<HTMLElement>('button:not([disabled]), a[href], [tabindex]:not([tabindex="-1"])');
    firstInteractive?.focus();
}
