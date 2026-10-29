import { TestBed } from '@angular/core/testing';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { utcInstant } from '../../../../shared/models/semantics/date-value';
import { entityId } from '../../../../shared/models/semantics/entity-id';
import { HydrationDetailsDialogComponent } from './hydration-details-dialog';

const HISTORY_LIMIT = 10;
const ID_SUFFIX_LENGTH = 12;
const ENTRY_AMOUNT_ML = 150;
const PARTIAL_TOTAL_ML = 750;
const ABOVE_GOAL_TOTAL_ML = 2500;

describe('HydrationDetailsDialogComponent', () => {
    it.each([0, 1, HISTORY_LIMIT, HISTORY_LIMIT + 1])(
        'shows timed entries with a truncation hint only when more than ten exist (%s)',
        async count => {
            const timestamp = utcInstant('2026-10-28T20:15:00Z');
            const entries = Array.from({ length: Math.min(count, HISTORY_LIMIT) }, (_, index) => ({
                id: entityId<'hydration-entry'>(`00000000-0000-4000-8000-${String(index + 1).padStart(ID_SUFFIX_LENGTH, '0')}`),
                timestampUtc: timestamp,
                amountMl: ENTRY_AMOUNT_ML + index,
            }));
            await TestBed.configureTestingModule({
                imports: [HydrationDetailsDialogComponent],
                providers: [
                    provideTranslateTesting(),
                    {
                        provide: FD_UI_DIALOG_DATA,
                        useValue: { total: 2000, goal: 2000, history: { entries, hasMore: count > HISTORY_LIMIT } },
                    },
                    { provide: FdUiDialogRef, useValue: { close: vi.fn() } },
                ],
            }).compileComponents();
            const fixture = TestBed.createComponent(HydrationDetailsDialogComponent);
            fixture.detectChanges();
            const host = fixture.nativeElement as HTMLElement;
            const rows = host.querySelectorAll('.hydration-history__entry');
            expect(rows).toHaveLength(Math.min(count, HISTORY_LIMIT));
            expect(host.textContent.includes('HYDRATION_CARD.HISTORY_LIMIT')).toBe(count > HISTORY_LIMIT);
            expect(host.textContent.includes('HYDRATION_CARD.HISTORY_EMPTY')).toBe(count === 0);
            if (count > 0) {
                const local = new Date(timestamp);
                const expectedTime = `${String(local.getHours()).padStart(2, '0')}:${String(local.getMinutes()).padStart(2, '0')}`;
                expect(rows[0].querySelector('time')?.textContent).toBe(expectedTime);
                expect(rows[0].querySelector('time')?.getAttribute('datetime')).toBe(timestamp);
                expect(rows[0].textContent).toContain('150');
            }
        },
    );

    it.each([0, PARTIAL_TOTAL_ML, ABOVE_GOAL_TOTAL_ML])(
        'shows a compact selected-day total for %s ml and retains footer actions',
        async total => {
            const close = vi.fn();
            await TestBed.configureTestingModule({
                imports: [HydrationDetailsDialogComponent],
                providers: [
                    provideTranslateTesting(),
                    { provide: FD_UI_DIALOG_DATA, useValue: { total } },
                    { provide: FdUiDialogRef, useValue: { close } },
                ],
            }).compileComponents();
            const fixture = TestBed.createComponent(HydrationDetailsDialogComponent);
            fixture.detectChanges();
            const host = fixture.nativeElement as HTMLElement;
            expect(host.querySelector('dd')?.textContent).toContain(new Intl.NumberFormat('en').format(total));
            expect(host.querySelectorAll('dd')).toHaveLength(1);
            const buttons = host.querySelectorAll<HTMLButtonElement>('[fdUiDialogFooter] button');
            buttons[0].click();
            expect(close).toHaveBeenCalledWith(true);
            buttons[1].click();
            expect(close).toHaveBeenLastCalledWith();
        },
    );
});
