import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { Subject } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { WeightHistoryFacade } from '../../lib/weight-history.facade';
import type { WeightEntry } from '../../models/weight-entry.data';
import { WeightHistoryEntriesDialogComponent } from './weight-history-entries-dialog';

const PAGE_SIZE = 20;
const BASE_MEASUREMENT = 80;
const FETCH_SIZE = PAGE_SIZE + 1;
function setup(): {
    fixture: ComponentFixture<WeightHistoryEntriesDialogComponent>;
    component: WeightHistoryEntriesDialogComponent;
    fetch: ReturnType<typeof vi.fn>;
    responses: Array<Subject<WeightEntry[]>>;
    close: ReturnType<typeof vi.fn>;
} {
    const responses: Array<Subject<WeightEntry[]>> = [];
    const fetch = vi.fn(() => {
        const response = new Subject<WeightEntry[]>();
        responses.push(response);
        return response;
    });
    const close = vi.fn();
    TestBed.configureTestingModule({
        imports: [WeightHistoryEntriesDialogComponent],
        providers: [
            provideTranslateTesting(),
            { provide: FD_UI_DIALOG_DATA, useValue: { currentWeight: 80, desiredWeightKg: 75 } },
            { provide: FdUiDialogRef, useValue: { close } },
            { provide: WeightHistoryFacade, useValue: { getEntryHistoryPage: fetch } },
        ],
    });
    const fixture = TestBed.createComponent(WeightHistoryEntriesDialogComponent);
    fixture.detectChanges();
    return { fixture, component: fixture.componentInstance, fetch, responses, close };
}
function page(): WeightEntry[] {
    return Array.from({ length: FETCH_SIZE }, (_, index) => ({
        id: String(index),
        userId: 'u',
        date: `2026-09-${String(FETCH_SIZE - index).padStart(2, '0')}`,
        weightKg: BASE_MEASUREMENT + index,
    }));
}
describe('Weight measurement dialog pagination', () => {
    it('shows 20 rows with the last delta, then appends the final row and passes it to editing', () => {
        const { fixture, component, responses, fetch, close } = setup();
        responses[0].next(page());
        responses[0].complete();
        fixture.detectChanges();
        expect(component['items']()).toHaveLength(PAGE_SIZE);
        expect(component['items']().at(-1)?.change).toBe(-1);
        const root = fixture.nativeElement as HTMLElement;
        const more = Array.from(root.querySelectorAll('button')).find(button =>
            button.textContent.includes('MEASUREMENT_HISTORY_PAGING.MORE'),
        );
        more?.click();
        expect(fetch).toHaveBeenLastCalledWith('2026-09-01');
        const oldest = page()[PAGE_SIZE];
        responses[1].next([oldest]);
        responses[1].complete();
        fixture.detectChanges();
        expect(component['items']()).toHaveLength(FETCH_SIZE);
        expect(component['items']().at(-1)?.change).toBeNull();
        expect(root.textContent).not.toContain('MEASUREMENT_HISTORY_PAGING.MORE');
        root.querySelectorAll<HTMLButtonElement>('button[aria-label="WEIGHT_HISTORY.EDIT"]').item(PAGE_SIZE).click();
        expect(close).toHaveBeenCalledWith({ action: 'edit', entry: oldest });
    });
    it('renders retry rather than empty on failure, and cancels pending work when closed', () => {
        const { fixture, responses, fetch } = setup();
        responses[0].error(new Error('offline'));
        fixture.detectChanges();
        const root = fixture.nativeElement as HTMLElement;
        expect(root.querySelector('[role="alert"]')?.textContent).toContain('MEASUREMENT_HISTORY_PAGING.ERROR');
        expect(root.textContent).not.toContain('MEASUREMENT_HISTORY_PAGING.EMPTY');
        Array.from(root.querySelectorAll('button'))
            .find(button => button.textContent.includes('MEASUREMENT_HISTORY_PAGING.RETRY'))
            ?.click();
        expect(fetch).toHaveBeenCalledTimes(2);
        fixture.destroy();
        expect(responses[1].observed).toBe(false);
    });
});
