import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { Subject } from 'rxjs';
import { describe, expect, it, type Mock, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import type { GoalHistoryPage, WeightGoalHistoryItem } from '../../../../shared/models/user.data';
import { WeightHistoryFacade } from '../../lib/weight-history.facade';
import { WeightGoalHistoryDialogComponent } from './weight-goal-history-dialog';

const CURRENT_MEASUREMENT = 80;
const TOTAL_LOADED_ROWS = 3;
const active: WeightGoalHistoryItem = {
    id: 'active',
    startWeightKg: 90,
    targetWeightKg: 70,
    endWeightKg: null,
    startedAtUtc: '2026-08-01',
    endedAtUtc: null,
    status: 'Active',
};
const closed: WeightGoalHistoryItem = { ...active, id: 'closed', status: 'Cancelled', endedAtUtc: '2026-08-01' };
function setup(): {
    fixture: ComponentFixture<WeightGoalHistoryDialogComponent>;
    request: Mock;
    pages: Array<Subject<GoalHistoryPage<WeightGoalHistoryItem>>>;
} {
    const pages: Array<Subject<GoalHistoryPage<WeightGoalHistoryItem>>> = [];
    const request = vi.fn(() => {
        const page = new Subject<GoalHistoryPage<WeightGoalHistoryItem>>();
        pages.push(page);
        return page;
    });
    TestBed.configureTestingModule({
        imports: [WeightGoalHistoryDialogComponent],
        providers: [
            provideTranslateTesting(),
            {
                provide: WeightHistoryFacade,
                useValue: {
                    weightGoalHistory: signal([active, closed]),
                    latestWeight: signal(CURRENT_MEASUREMENT),
                    desiredWeightSaveVersion: signal(0),
                    getGoalHistoryPage: request,
                },
            },
            { provide: FdUiDialogRef, useValue: { close: vi.fn() } },
        ],
    });
    const fixture = TestBed.createComponent(WeightGoalHistoryDialogComponent);
    fixture.detectChanges();
    return { fixture, request, pages };
}

describe('Weight history pagination wiring', () => {
    it('pins the active goal and appends requested pages through the rendered button', () => {
        const { fixture, request, pages } = setup();
        const root = fixture.nativeElement as HTMLElement;
        expect(request).toHaveBeenCalledExactlyOnceWith(undefined);
        expect(root.querySelectorAll('article')).toHaveLength(1);
        pages[0].next({ items: [closed], nextCursor: 'second' });
        pages[0].complete();
        fixture.detectChanges();
        expect(root.querySelectorAll('article')).toHaveLength(2);
        const more = root.querySelector<HTMLButtonElement>('fd-ui-button button');
        expect(more?.textContent).toContain('GOAL_HISTORY_PAGING.MORE');
        more?.click();
        fixture.detectChanges();
        expect(request).toHaveBeenLastCalledWith('second');
        pages[1].next({ items: [{ ...closed, id: 'older' }], nextCursor: null });
        pages[1].complete();
        fixture.detectChanges();
        expect(root.querySelectorAll('article')).toHaveLength(TOTAL_LOADED_ROWS);
        expect(root.querySelector('article')?.textContent).toContain('WEIGHT_HISTORY.GOAL_STATUS_ACTIVE');
        expect(root.querySelector('fd-ui-button')).toBeNull();
    });

    it('announces a fetch error and lets the user retry without losing the active goal', () => {
        const { fixture, request, pages } = setup();
        const root = fixture.nativeElement as HTMLElement;
        pages[0].error(new Error('offline'));
        fixture.detectChanges();
        expect(root.querySelector('[role="alert"]')?.textContent).toContain('GOAL_HISTORY_PAGING.ERROR');
        const retry = root.querySelector<HTMLButtonElement>('fd-ui-button button');
        expect(retry?.textContent).toContain('GOAL_HISTORY_PAGING.RETRY');
        retry?.click();
        fixture.detectChanges();
        expect(request).toHaveBeenCalledTimes(2);
        expect(request).toHaveBeenLastCalledWith(undefined);
        pages[1].next({ items: [], nextCursor: null });
        pages[1].complete();
        fixture.detectChanges();
        expect(root.querySelector('[role="alert"]')).toBeNull();
        expect(root.querySelectorAll('article')).toHaveLength(1);
        expect(root.textContent).not.toContain('GOAL_HISTORY_PAGING.EMPTY');
    });
});
