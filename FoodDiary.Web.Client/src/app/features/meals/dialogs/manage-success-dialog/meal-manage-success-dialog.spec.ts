import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { type ConsumptionManageSuccessDialogData, MealManageSuccessDialogComponent } from './meal-manage-success-dialog';

const DEFAULT_DIALOG_DATA = { isEdit: false };

describe('MealManageSuccessDialogComponent', () => {
    let component: MealManageSuccessDialogComponent;
    let fixture: ComponentFixture<MealManageSuccessDialogComponent>;
    let dialogRefSpy: { close: ReturnType<typeof vi.fn> };

    function createComponent(data: ConsumptionManageSuccessDialogData = DEFAULT_DIALOG_DATA): void {
        dialogRefSpy = { close: vi.fn() };

        TestBed.configureTestingModule({
            imports: [MealManageSuccessDialogComponent],
            providers: [
                provideTranslateTesting(),
                { provide: FdUiDialogRef, useValue: dialogRefSpy },
                { provide: FD_UI_DIALOG_DATA, useValue: data },
            ],
        });

        fixture = TestBed.createComponent(MealManageSuccessDialogComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    }

    it('should create', () => {
        createComponent();
        expect(component).toBeTruthy();
    });

    it('should close with Home', () => {
        createComponent();
        component['close']('Home');
        expect(dialogRefSpy.close).toHaveBeenCalledWith('Home');
    });

    it('should close with ConsumptionList', () => {
        createComponent();
        component['close']('ConsumptionList');
        expect(dialogRefSpy.close).toHaveBeenCalledWith('ConsumptionList');
    });

    it('should display edit title when isEdit true', () => {
        createComponent({ isEdit: true });
        expect(component['data'].isEdit).toBe(true);
    });
});
