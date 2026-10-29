import { TestBed } from '@angular/core/testing';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { HydrationDetailsDialogComponent } from './hydration-details-dialog';

describe('HydrationDetailsDialogComponent', () => {
    it.each([
        { total: 750, goal: 2000, expected: 'HYDRATION_CARD.REMAINING' },
        { total: 2500, goal: 2000, expected: 'HYDRATION_CARD.GOAL_REACHED' },
        { total: 750, goal: null, expected: 'HYDRATION_CARD.GOAL_BODY' },
        { total: 750, goal: 0, expected: 'HYDRATION_CARD.GOAL_BODY' },
    ])('shows a selected-day summary for $total ml and goal $goal', async ({ total, goal, expected }) => {
        const close = vi.fn();
        await TestBed.configureTestingModule({
            imports: [HydrationDetailsDialogComponent],
            providers: [
                provideTranslateTesting(),
                { provide: FD_UI_DIALOG_DATA, useValue: { total, goal } },
                { provide: FdUiDialogRef, useValue: { close } },
            ],
        }).compileComponents();
        const fixture = TestBed.createComponent(HydrationDetailsDialogComponent);
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        expect(host.textContent).toContain(expected);
        expect(host.querySelector('dd')?.textContent).toContain(new Intl.NumberFormat('en').format(total));
        expect(host.querySelectorAll('dd')).toHaveLength(goal !== null && goal > 0 ? 2 : 1);
        const buttons = host.querySelectorAll<HTMLButtonElement>('[fdUiDialogFooter] button');
        buttons[0].click();
        expect(close).toHaveBeenCalledWith(true);
        buttons[1].click();
        expect(close).toHaveBeenLastCalledWith();
    });
});
