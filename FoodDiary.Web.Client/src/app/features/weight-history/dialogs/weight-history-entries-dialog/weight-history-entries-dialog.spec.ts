import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { afterEach, describe, expect, it, type Mock, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import type { WeightEntry } from '../../models/weight-entry.data';
import { WeightHistoryEntriesDialogComponent } from './weight-history-entries-dialog';

const FIXTURE_YEAR = 2026;
const FIXTURE_APRIL_INDEX = 3;
const FIXTURE_NOON_HOUR = 12;
const FIXTURE_REFERENCE_MEASUREMENT = 80;
const FIXTURE_PREVIOUS_MEASUREMENT = 82;
const FIXTURE_NEGATIVE_2 = -2;
const entry = (id: string, date: string, value: number): WeightEntry => ({ id, userId: 'u', date, weightKg: value });
function setup(entries: WeightEntry[]): {
    fixture: ComponentFixture<WeightHistoryEntriesDialogComponent>;
    component: WeightHistoryEntriesDialogComponent;
    close: Mock;
} {
    const close = vi.fn();
    TestBed.configureTestingModule({
        imports: [WeightHistoryEntriesDialogComponent],
        providers: [
            provideTranslateTesting(),
            { provide: FD_UI_DIALOG_DATA, useValue: { entries, currentWeight: 80, desiredWeightKg: 75 } },
            { provide: FdUiDialogRef, useValue: { close } },
        ],
    });
    const fixture = TestBed.createComponent(WeightHistoryEntriesDialogComponent);
    fixture.detectChanges();
    return { fixture, component: fixture.componentInstance, close };
}
afterEach(() => vi.useRealTimers());
describe('Weight all entries dialog', () => {
    it('preserves newest-first rows, calculates adjacent changes, and labels today', () => {
        vi.useFakeTimers();
        vi.setSystemTime(new Date(FIXTURE_YEAR, FIXTURE_APRIL_INDEX, 2, FIXTURE_NOON_HOUR));
        const { component } = setup([
            entry('new', '2026-04-02', FIXTURE_REFERENCE_MEASUREMENT),
            entry('old', '2026-04-01', FIXTURE_PREVIOUS_MEASUREMENT),
        ]);
        expect(component['items']().map(x => [x.entry.id, x.change, x.isToday])).toEqual([
            ['new', FIXTURE_NEGATIVE_2, true],
            ['old', null, false],
        ]);
    });
    it.each(['edit', 'remove'] as const)('returns selected entry for %s', action => {
        const value = entry('one', '2026-04-01', FIXTURE_REFERENCE_MEASUREMENT);
        const { fixture, close } = setup([value]);
        const root = fixture.nativeElement as HTMLElement;
        root.querySelector<HTMLButtonElement>(`button[aria-label="WEIGHT_HISTORY.${action === 'edit' ? 'EDIT' : 'DELETE'}"]`)?.click();
        expect(close).toHaveBeenCalledWith({ action, entry: value });
    });
    it('handles an empty list', () => {
        expect(setup([]).component['items']()).toEqual([]);
    });
});
