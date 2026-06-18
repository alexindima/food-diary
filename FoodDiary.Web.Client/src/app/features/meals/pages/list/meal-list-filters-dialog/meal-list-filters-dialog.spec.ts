import { type ComponentFixture, TestBed } from '@angular/core/testing';
import type { FdUiDateRangeValue } from 'fd-ui-kit';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import { MealListFiltersDialogComponent } from './meal-list-filters-dialog';

const dateRange: FdUiDateRangeValue = {
    start: new Date('2026-05-01T00:00:00Z'),
    end: new Date('2026-05-14T00:00:00Z'),
};

describe('MealListFiltersDialogComponent', () => {
    it('should initialize date range from dialog data', async () => {
        const { component } = await setupComponentAsync(dateRange);

        expect(component['formModel']().dateRange).toEqual(dateRange);
    });

    it('should reset date range', async () => {
        const { component } = await setupComponentAsync(dateRange);

        component['onReset']();

        expect(component['formModel']().dateRange).toBeNull();
    });

    it('should close with selected filters', async () => {
        const { component, dialogRef } = await setupComponentAsync(null);
        component['form'].dateRange().value.set(dateRange);

        component['onApply']();

        expect(dialogRef.close).toHaveBeenCalledWith({ dateRange });
    });

    it('should cancel native submit and delegate to FormRoot submission', async () => {
        const { component, dialogRef, fixture } = await setupComponentAsync(null);
        const formElement = (fixture.nativeElement as HTMLElement).querySelector('form');
        const submitEvent = new Event('submit', { bubbles: true, cancelable: true });
        component['form'].dateRange().value.set(dateRange);

        const wasNotCancelled = formElement?.dispatchEvent(submitEvent);
        await fixture.whenStable();

        expect(formElement).not.toBeNull();
        expect(wasNotCancelled).toBe(false);
        expect(submitEvent.defaultPrevented).toBe(true);
        expect(dialogRef.close).toHaveBeenCalledWith({ dateRange });
    });

    it('should close with null on cancel', async () => {
        const { component, dialogRef } = await setupComponentAsync(dateRange);

        component['onCancel']();

        expect(dialogRef.close).toHaveBeenCalledWith(null);
    });
});

async function setupComponentAsync(initialDateRange: FdUiDateRangeValue | null): Promise<{
    component: MealListFiltersDialogComponent;
    dialogRef: { close: ReturnType<typeof vi.fn> };
    fixture: ComponentFixture<MealListFiltersDialogComponent>;
}> {
    const dialogRef = {
        close: vi.fn(),
    };

    await TestBed.resetTestingModule()
        .configureTestingModule({
            imports: [MealListFiltersDialogComponent],
            providers: [
                provideTranslateTesting(),
                { provide: FdUiDialogRef, useValue: dialogRef },
                { provide: FD_UI_DIALOG_DATA, useValue: { dateRange: initialDateRange } },
            ],
        })
        .compileComponents();

    const fixture = TestBed.createComponent(MealListFiltersDialogComponent);
    fixture.detectChanges();

    return {
        component: fixture.componentInstance,
        dialogRef,
        fixture,
    };
}
