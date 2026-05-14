import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import type { FdUiDateRangeValue } from 'fd-ui-kit';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { describe, expect, it, vi } from 'vitest';

import { MealListFiltersDialogComponent } from './meal-list-filters-dialog.component';

const dateRange: FdUiDateRangeValue = {
    start: new Date('2026-05-01T00:00:00Z'),
    end: new Date('2026-05-14T00:00:00Z'),
};

describe('MealListFiltersDialogComponent', () => {
    it('should initialize date range from dialog data', async () => {
        const { component } = await setupComponentAsync(dateRange);

        expect(component.dateRangeControl.value).toEqual(dateRange);
    });

    it('should reset date range', async () => {
        const { component } = await setupComponentAsync(dateRange);

        component.onReset();

        expect(component.dateRangeControl.value).toBeNull();
    });

    it('should close with selected filters and prevent form submit default', async () => {
        const { component, dialogRef } = await setupComponentAsync(null);
        const preventDefault = vi.fn();
        component.dateRangeControl.setValue(dateRange);

        component.onApply({ preventDefault } as unknown as Event);

        expect(preventDefault).toHaveBeenCalled();
        expect(dialogRef.close).toHaveBeenCalledWith({ dateRange });
    });

    it('should close with null on cancel', async () => {
        const { component, dialogRef } = await setupComponentAsync(dateRange);

        component.onCancel();

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
            imports: [MealListFiltersDialogComponent, TranslateModule.forRoot()],
            providers: [
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
