import { describe, expect, it } from 'vitest';

import {
    buildDashboardBlockState,
    buildDashboardHeaderState,
    buildDashboardMealsPreviewState,
    isDashboardAsideBlock,
} from './dashboard-view-state.mapper';

describe('dashboard view state mapper', () => {
    it('builds today and historical header states', () => {
        expect(buildDashboardHeaderState(true, 'May 16, 2026')).toEqual({
            fullTitleKey: 'DASHBOARD.TITLE',
            compactTitleKey: 'DASHBOARD.TITLE_SHORT',
            titleParams: null,
            selectedDateLabel: 'May 16, 2026',
        });

        expect(buildDashboardHeaderState(false, 'May 15, 2026')).toEqual({
            fullTitleKey: 'DASHBOARD.TITLE_FOR_DATE',
            compactTitleKey: 'DASHBOARD.TITLE_FOR_DATE_SHORT',
            titleParams: { date: 'May 15, 2026' },
            selectedDateLabel: 'May 15, 2026',
        });
    });

    it('builds meal preview state for today and selected dates', () => {
        expect(buildDashboardMealsPreviewState(true, 'Meals on date')).toEqual({
            titleText: null,
            emptyKey: 'DASHBOARD.MEALS_EMPTY',
            showDateActions: true,
            showEmptyState: false,
        });

        expect(buildDashboardMealsPreviewState(false, 'Meals on date')).toEqual({
            titleText: 'Meals on date',
            emptyKey: 'DASHBOARD.MEALS_EMPTY_FOR_DATE',
            showDateActions: false,
            showEmptyState: true,
        });
    });

    it('builds accessible block state for editing and non-editing modes', () => {
        expect(
            buildDashboardBlockState({
                blockId: 'summary',
                editing: true,
                isVisible: true,
                canToggle: false,
                ariaLabel: null,
                stateOptions: { locked: true },
            }),
        ).toMatchObject({
            role: 'button',
            tabIndex: 0,
            ariaPressed: true,
            ariaDisabled: true,
            inert: '',
        });

        expect(
            buildDashboardBlockState({
                blockId: 'tdee',
                editing: false,
                isVisible: true,
                canToggle: true,
                ariaLabel: 'Open TDEE',
                stateOptions: { alwaysInteractive: true },
            }),
        ).toMatchObject({
            role: 'button',
            tabIndex: 0,
            ariaPressed: null,
            ariaDisabled: null,
            ariaLabel: 'Open TDEE',
            inert: null,
        });
    });

    it('detects aside blocks', () => {
        expect(isDashboardAsideBlock('hydration')).toBe(true);
        expect(isDashboardAsideBlock('summary')).toBe(false);
    });
});
