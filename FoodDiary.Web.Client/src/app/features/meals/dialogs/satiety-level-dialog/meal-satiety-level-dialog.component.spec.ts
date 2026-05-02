import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TranslateModule } from '@ngx-translate/core';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { describe, expect, it, vi } from 'vitest';

import { MealSatietyLevelDialogComponent, SatietyLevelDialogData } from './meal-satiety-level-dialog.component';

describe('MealSatietyLevelDialogComponent', () => {
    let component: MealSatietyLevelDialogComponent;
    let fixture: ComponentFixture<MealSatietyLevelDialogComponent>;
    let dialogRefSpy: { close: ReturnType<typeof vi.fn> };

    function createComponent(data: SatietyLevelDialogData = { titleKey: 'TITLE', value: null }): void {
        dialogRefSpy = { close: vi.fn() };

        TestBed.configureTestingModule({
            imports: [MealSatietyLevelDialogComponent, TranslateModule.forRoot()],
            providers: [
                provideNoopAnimations(),
                { provide: FdUiDialogRef, useValue: dialogRefSpy },
                { provide: FD_UI_DIALOG_DATA, useValue: data },
            ],
        });

        fixture = TestBed.createComponent(MealSatietyLevelDialogComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    }

    it('should create', () => {
        createComponent();
        expect(component).toBeTruthy();
    });

    it('should initialize with value from data', () => {
        createComponent({ titleKey: 'TITLE', value: 5 });
        expect(component.selectedValue()).toBe(5);
    });

    it('should initialize with neutral value when data value is null', () => {
        createComponent({ titleKey: 'TITLE', value: null });
        expect(component.selectedValue()).toBe(3);
    });

    it('should update selectedValue on selection', () => {
        createComponent();
        component.onValueSelected(4);
        expect(component.selectedValue()).toBe(4);
    });

    it('should close with selected value', () => {
        createComponent({ titleKey: 'TITLE', value: 3 });
        component.onValueSelected(5);
        component.closeWithValue();
        expect(dialogRefSpy.close).toHaveBeenCalledWith(5);
    });

    it('should close with null on dismiss', () => {
        createComponent();
        component.close();
        expect(dialogRefSpy.close).toHaveBeenCalledWith();
    });
});
