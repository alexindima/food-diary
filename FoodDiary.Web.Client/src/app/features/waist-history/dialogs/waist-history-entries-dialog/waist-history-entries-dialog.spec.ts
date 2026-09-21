import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { of } from 'rxjs';
import { afterEach, describe, expect, it, type Mock, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { WaistHistoryFacade } from '../../lib/waist-history.facade';
import type { WaistEntry } from '../../models/waist-entry.data';
import { WaistHistoryEntriesDialogComponent } from './waist-history-entries-dialog';

const FIXTURE_YEAR = 2026;
const FIXTURE_APRIL_INDEX = 3;
const FIXTURE_NOON_HOUR = 12;
const FIXTURE_REFERENCE_MEASUREMENT = 80;
const FIXTURE_PREVIOUS_MEASUREMENT = 82;
const FIXTURE_NEGATIVE_2 = -2;
const entry = (id: string, date: string, value: number): WaistEntry => ({ id, userId: 'u', date, circumferenceCm: value });
function setup(entries: WaistEntry[]): {
    fixture: ComponentFixture<WaistHistoryEntriesDialogComponent>;
    component: WaistHistoryEntriesDialogComponent;
    close: Mock;
} {
    const close = vi.fn();
    TestBed.configureTestingModule({
        imports: [WaistHistoryEntriesDialogComponent],
        providers: [
            provideTranslateTesting(),
            { provide: WaistHistoryFacade, useValue: { getEntryHistoryPage: vi.fn().mockReturnValue(of(entries)) } },
            { provide: FD_UI_DIALOG_DATA, useValue: { entries, currentWaist: 80, desiredWaistCm: 75 } },
            { provide: FdUiDialogRef, useValue: { close } },
        ],
    });
    const fixture = TestBed.createComponent(WaistHistoryEntriesDialogComponent);
    fixture.detectChanges();
    return { fixture, component: fixture.componentInstance, close };
}
afterEach(() => vi.useRealTimers());
describe('Waist all entries dialog', () => {
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
        root.querySelector<HTMLButtonElement>(`button[aria-label="WAIST_HISTORY.${action === 'edit' ? 'EDIT' : 'DELETE'}"]`)?.click();
        expect(close).toHaveBeenCalledWith({ action, entry: value });
    });
    it('handles an empty list', () => {
        expect(setup([]).component['items']()).toEqual([]);
    });
});

describe('Waist measurement change semantics', () => {
    it('distinguishes an unchanged measurement from missing comparison and makes the hint keyboard reachable', () => {
        const { fixture } = setup([
            entry('new', '2026-04-02', FIXTURE_REFERENCE_MEASUREMENT),
            entry('old', '2026-04-01', FIXTURE_REFERENCE_MEASUREMENT),
        ]);
        const root = fixture.nativeElement as HTMLElement;
        const changes = root.querySelectorAll<HTMLElement>('.waist-history-page__entry-change');
        expect(changes[0].textContent.trim()).toMatch(/^0\s/);
        expect(changes[0].textContent).not.toContain('\u2014');
        expect(changes[0].getAttribute('tabindex')).toBe('0');
        expect(changes[1].textContent.trim()).toBe('\u2014');
        expect(changes[1].hasAttribute('tabindex')).toBe(false);
    });
});
