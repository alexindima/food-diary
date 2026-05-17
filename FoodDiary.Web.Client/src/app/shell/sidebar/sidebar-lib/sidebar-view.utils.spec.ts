import { describe, expect, it, vi } from 'vitest';

import {
    buildPrimarySidebarNavItems,
    calculateSidebarDailyProgressPercent,
    focusFirstSidebarInteractiveElement,
    getSidebarMobileSheetLabelKey,
    getSidebarMobileSheetRouteItems,
    isSidebarActionItem,
    isSidebarRouteActive,
    isSidebarRouteItem,
    normalizeSidebarPath,
} from './sidebar-view.utils';

const PERCENT_MAX = 100;
const HALF_CONSUMED = 50;
const GOAL = 100;
const OVER_GOAL_CONSUMED = 150;
const EMPTY_GOAL = 0;
const NEGATIVE_CONSUMED = -50;

describe('sidebar view utils', () => {
    it('builds primary navigation by role', () => {
        const adminItems = buildPrimarySidebarNavItems(true, true);

        expect(adminItems.map(item => item.id)).toEqual(['dashboard', 'dietologist', 'admin']);
        expect(adminItems.filter(isSidebarRouteItem).map(item => item.id)).toEqual(['dashboard', 'dietologist']);
        expect(adminItems.filter(isSidebarActionItem).map(item => item.id)).toEqual(['admin']);
    });

    it('normalizes paths and matches routes', () => {
        expect(normalizeSidebarPath('/meals?date=today#top')).toBe('/meals');
        expect(normalizeSidebarPath('')).toBe('/');
        expect(isSidebarRouteActive('/meals/123?tab=info', '/meals', false)).toBe(true);
        expect(isSidebarRouteActive('/meals/123', '/meals', true)).toBe(false);
    });

    it('calculates clamped daily progress', () => {
        expect(calculateSidebarDailyProgressPercent(HALF_CONSUMED, GOAL)).toBe(HALF_CONSUMED);
        expect(calculateSidebarDailyProgressPercent(OVER_GOAL_CONSUMED, GOAL)).toBe(PERCENT_MAX);
        expect(calculateSidebarDailyProgressPercent(HALF_CONSUMED, EMPTY_GOAL)).toBe(EMPTY_GOAL);
        expect(calculateSidebarDailyProgressPercent(NEGATIVE_CONSUMED, GOAL)).toBe(EMPTY_GOAL);
    });

    it('resolves mobile sheet labels and route item groups', () => {
        const food = [{ id: 'meals', icon: 'restaurant', labelKey: 'SIDEBAR.FOOD_DIARY', route: '/meals' }];
        const body = [{ id: 'weight', icon: 'monitor_weight', labelKey: 'SIDEBAR.WEIGHT', route: '/weight-history' }];
        const reports = [{ id: 'statistics', icon: 'bar_chart', labelKey: 'SIDEBAR.REPORTS', route: '/statistics' }];

        expect(getSidebarMobileSheetLabelKey('food')).toBe('SIDEBAR.FOOD_TRACKING');
        expect(getSidebarMobileSheetLabelKey(null)).toBe('');
        expect(getSidebarMobileSheetRouteItems('body', food, body, reports)).toBe(body);
        expect(getSidebarMobileSheetRouteItems('user', food, body, reports)).toEqual([]);
    });

    it('focuses the first interactive element when available', () => {
        const container = document.createElement('div');
        const button = document.createElement('button');
        const focusSpy = vi.spyOn(button, 'focus');
        container.append(button);

        focusFirstSidebarInteractiveElement(container);

        expect(focusSpy).toHaveBeenCalledOnce();
    });
});
