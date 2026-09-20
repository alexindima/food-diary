import { signal, type WritableSignal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { describe, expect, it, type Mock, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import type { WaistGoalHistoryItem } from '../../../../shared/models/user.data';
import { WaistHistoryFacade } from '../../lib/waist-history.facade';
import { WaistGoalHistoryDialogComponent } from './waist-goal-history-dialog';

const FIXTURE_RECENT_MEASUREMENT = 90;
const FIXTURE_UPPER_MEASUREMENT = 100;
const FIXTURE_REFERENCE_MEASUREMENT = 80;
const FIXTURE_AWAY_MEASUREMENT = 110;
const FIXTURE_LOWER_MEASUREMENT = 70;
const FIXTURE_HALF_PROGRESS = 50;
function setup({
    status = 'Active',
    current = FIXTURE_RECENT_MEASUREMENT,
    end = null,
    start = FIXTURE_UPPER_MEASUREMENT,
    target = FIXTURE_REFERENCE_MEASUREMENT,
}: {
    status?: WaistGoalHistoryItem['status'];
    current?: number | null;
    end?: number | null;
    start?: number;
    target?: number;
} = {}): {
    fixture: ComponentFixture<WaistGoalHistoryDialogComponent>;
    component: WaistGoalHistoryDialogComponent;
    history: WritableSignal<WaistGoalHistoryItem[]>;
    close: Mock;
} {
    const history = signal<WaistGoalHistoryItem[]>([
        {
            id: 'goal',
            startWaistCm: start,
            targetWaistCm: target,
            endWaistCm: end,
            startedAtUtc: '2026-01-01',
            endedAtUtc: status === 'Active' ? null : '2026-02-01',
            status,
        },
    ]);
    const close = vi.fn();
    TestBed.configureTestingModule({
        imports: [WaistGoalHistoryDialogComponent],
        providers: [
            provideTranslateTesting(),
            { provide: WaistHistoryFacade, useValue: { waistGoalHistory: history, latestWaist: signal(current) } },
            { provide: FdUiDialogRef, useValue: { close } },
        ],
    });
    const fixture = TestBed.createComponent(WaistGoalHistoryDialogComponent);
    fixture.detectChanges();
    return { fixture, component: fixture.componentInstance, history, close };
}
describe('Waist goal history dialog', () => {
    it.each([
        { status: 'Active', current: 90, end: null, change: -10, progress: 50 },
        { status: 'Cancelled', current: 70, end: 95, change: -5, progress: null },
        { status: 'Replaced', current: 70, end: null, change: 0, progress: null },
        { status: 'Active', current: null, end: null, change: 0, progress: 0 },
    ] as const)('keeps $status lifecycle measurements separate', ({ status, current, end, change, progress }) => {
        const { component } = setup({ status, current, end });
        expect(component['goals']()[0]).toMatchObject({ change, progress, statusKey: `WAIST_HISTORY.GOAL_STATUS_${status.toUpperCase()}` });
    });
    it.each([
        [FIXTURE_UPPER_MEASUREMENT, FIXTURE_REFERENCE_MEASUREMENT, FIXTURE_AWAY_MEASUREMENT, 0],
        [FIXTURE_UPPER_MEASUREMENT, FIXTURE_REFERENCE_MEASUREMENT, FIXTURE_LOWER_MEASUREMENT, FIXTURE_UPPER_MEASUREMENT],
        [FIXTURE_REFERENCE_MEASUREMENT, FIXTURE_UPPER_MEASUREMENT, FIXTURE_RECENT_MEASUREMENT, FIXTURE_HALF_PROGRESS],
        [FIXTURE_REFERENCE_MEASUREMENT, FIXTURE_REFERENCE_MEASUREMENT, FIXTURE_REFERENCE_MEASUREMENT, FIXTURE_UPPER_MEASUREMENT],
    ])('bounds progress for start %s goal %s current %s', (start, target, current, percent) => {
        expect(setup({ current, start, target }).component['goals']()[0].progress).toBe(percent);
    });
    it('renders empty history and closes', () => {
        const { fixture, component, history, close } = setup();
        history.set([]);
        fixture.detectChanges();
        expect(component['goals']()).toEqual([]);
        component['close']();
        expect(close).toHaveBeenCalledOnce();
    });
    it('handles an invalid stored start date without crashing', () => {
        const { fixture, component, history } = setup();
        history.update(goals => goals.map(g => ({ ...g, startedAtUtc: 'invalid' })));
        fixture.detectChanges();
        expect(component['goals']()[0].startDate).toBe('');
    });
});
