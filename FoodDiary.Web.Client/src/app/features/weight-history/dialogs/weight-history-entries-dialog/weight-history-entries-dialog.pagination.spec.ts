import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { Subject } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { calendarDate } from '../../../../shared/models/semantics/date-value';
import { entityId } from '../../../../shared/models/semantics/entity-id';
import type { WeightEntry } from '../../../../shared/models/weight-entry.data';
import { WeightHistoryFacade } from '../../lib/weight-history.facade';
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
    return Array.from({ length: FETCH_SIZE }, (_, index) => {
        const day = index === PAGE_SIZE ? '2025-12-31' : `2026-01-${String(PAGE_SIZE - index).padStart(2, '0')}`;
        return {
            id: entityId<'weight-entry'>(String(index)),
            userId: entityId<'user'>('u'),
            date: calendarDate(`${day}T00:00:00Z`),
            weightKg: BASE_MEASUREMENT + index,
        };
    });
}
describe('Weight measurement dialog pagination', () => {
    it('keeps the last delta across New Year and lets the user load and edit the December entry', async () => {
        const { fixture, component, responses, fetch, close } = setup();
        const entries = page();
        responses[0].next(entries);
        responses[0].complete();
        await fixture.whenStable();
        fixture.detectChanges();
        expect(component['items']()).toHaveLength(PAGE_SIZE);
        expect(component['items']().at(-1)?.change).toBe(-1);
        const root = fixture.nativeElement as HTMLElement;
        const more = Array.from(root.querySelectorAll('button')).find(button =>
            button.textContent.includes('MEASUREMENT_HISTORY_PAGING.MORE'),
        );
        more?.click();
        fixture.detectChanges();
        expect(fetch).toHaveBeenLastCalledWith('2025-12-31');
        const oldest = entries[PAGE_SIZE];
        responses[1].next([oldest]);
        responses[1].complete();
        await fixture.whenStable();
        fixture.detectChanges();
        expect(component['items']()).toHaveLength(FETCH_SIZE);
        expect(component['items']().at(-1)?.change).toBeNull();
        expect(root.textContent).not.toContain('MEASUREMENT_HISTORY_PAGING.MORE');
        root.querySelectorAll<HTMLButtonElement>('button[aria-label="WEIGHT_HISTORY.EDIT"]').item(PAGE_SIZE).click();
        expect(close).toHaveBeenCalledWith({ action: 'edit', entry: oldest });
    });
    it('renders retry rather than empty on failure, and cancels pending work when closed', async () => {
        const { fixture, responses, fetch } = setup();
        responses[0].error(new Error('offline'));
        await fixture.whenStable();
        fixture.detectChanges();
        const root = fixture.nativeElement as HTMLElement;
        expect(root.querySelector('[role="alert"]')?.textContent).toContain('MEASUREMENT_HISTORY_PAGING.ERROR');
        expect(root.textContent).not.toContain('MEASUREMENT_HISTORY_PAGING.EMPTY');
        Array.from(root.querySelectorAll('button'))
            .find(button => button.textContent.includes('MEASUREMENT_HISTORY_PAGING.RETRY'))
            ?.click();
        fixture.detectChanges();
        expect(fetch).toHaveBeenCalledTimes(2);
        fixture.destroy();
        expect(responses[1].observed).toBe(false);
    });
});
