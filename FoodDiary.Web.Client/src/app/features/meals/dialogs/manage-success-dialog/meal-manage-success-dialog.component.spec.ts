import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { TranslateModule } from '@ngx-translate/core';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { MealManageSuccessDialogComponent, ConsumptionManageSuccessDialogData } from './meal-manage-success-dialog.component';

describe('MealManageSuccessDialogComponent', () => {
    let component: MealManageSuccessDialogComponent;
    let fixture: ComponentFixture<MealManageSuccessDialogComponent>;
    let dialogRefSpy: { close: ReturnType<typeof vi.fn> };

    function createComponent(data: ConsumptionManageSuccessDialogData = { isEdit: false }): void {
        dialogRefSpy = { close: vi.fn() };

        TestBed.configureTestingModule({
            imports: [MealManageSuccessDialogComponent, TranslateModule.forRoot()],
            providers: [
                provideNoopAnimations(),
                { provide: MatDialogRef, useValue: dialogRefSpy },
                { provide: MAT_DIALOG_DATA, useValue: data },
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
        component.close('Home');
        expect(dialogRefSpy.close).toHaveBeenCalledWith('Home');
    });

    it('should close with ConsumptionList', () => {
        createComponent();
        component.close('ConsumptionList');
        expect(dialogRefSpy.close).toHaveBeenCalledWith('ConsumptionList');
    });

    it('should display edit title when isEdit true', () => {
        createComponent({ isEdit: true });
        expect(component.data.isEdit).toBe(true);
    });
});
