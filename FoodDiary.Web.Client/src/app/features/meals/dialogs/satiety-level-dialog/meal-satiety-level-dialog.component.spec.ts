import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { describe, expect, it, vi } from 'vitest';

import { MealSatietyLevelDialogComponent, type SatietyLevelDialogData } from './meal-satiety-level-dialog.component';

const LOW_SATIETY = 3;
const SELECTED_SATIETY = 4;
const HIGH_SATIETY = 5;

describe('MealSatietyLevelDialogComponent', () => {
    let component: MealSatietyLevelDialogComponent;
    let fixture: ComponentFixture<MealSatietyLevelDialogComponent>;
    let dialogRefSpy: { close: ReturnType<typeof vi.fn> };

    function createComponent(data: SatietyLevelDialogData = { titleKey: 'TITLE', value: null }): void {
        dialogRefSpy = { close: vi.fn() };

        TestBed.configureTestingModule({
            imports: [MealSatietyLevelDialogComponent, TranslateModule.forRoot()],
            providers: [
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
        createComponent({ titleKey: 'TITLE', value: HIGH_SATIETY });
        expect(component.selectedValue()).toBe(HIGH_SATIETY);
    });

    it('should initialize with neutral value when data value is null', () => {
        createComponent({ titleKey: 'TITLE', value: null });
        expect(component.selectedValue()).toBe(LOW_SATIETY);
    });

    it('should update selectedValue on selection', () => {
        createComponent();
        component.onValueSelected(SELECTED_SATIETY);
        expect(component.selectedValue()).toBe(SELECTED_SATIETY);
    });

    it('should close with selected value', () => {
        createComponent({ titleKey: 'TITLE', value: LOW_SATIETY });
        component.onValueSelected(HIGH_SATIETY);
        component.closeWithValue();
        expect(dialogRefSpy.close).toHaveBeenCalledWith(HIGH_SATIETY);
    });

    it('should close with null on dismiss', () => {
        createComponent();
        component.close();
        expect(dialogRefSpy.close).toHaveBeenCalledWith();
    });
});
