import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { TranslateModule } from '@ngx-translate/core';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import {
    MealSatietyLevelDialogComponent,
    SatietyLevelDialogData,
} from './meal-satiety-level-dialog.component';

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
                { provide: MatDialogRef, useValue: dialogRefSpy },
                { provide: MAT_DIALOG_DATA, useValue: data },
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

    it('should initialize with 0 when data value is null', () => {
        createComponent({ titleKey: 'TITLE', value: null });
        expect(component.selectedValue()).toBe(0);
    });

    it('should update selectedValue on selection', () => {
        createComponent();
        component.onValueSelected(7);
        expect(component.selectedValue()).toBe(7);
    });

    it('should close with selected value', () => {
        createComponent({ titleKey: 'TITLE', value: 3 });
        component.onValueSelected(8);
        component.closeWithValue();
        expect(dialogRefSpy.close).toHaveBeenCalledWith(8);
    });

    it('should close with null on dismiss', () => {
        createComponent();
        component.close();
        expect(dialogRefSpy.close).toHaveBeenCalledWith();
    });
});
