import { describe, expect, it, vi } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { TranslateModule } from '@ngx-translate/core';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { ProductSaveSuccessDialogComponent, ProductSaveSuccessDialogData } from './product-save-success-dialog.component';

describe('ProductSaveSuccessDialogComponent', () => {
    let component: ProductSaveSuccessDialogComponent;
    let fixture: ComponentFixture<ProductSaveSuccessDialogComponent>;
    let dialogRefSpy: { close: ReturnType<typeof vi.fn> };

    function createComponent(data: ProductSaveSuccessDialogData = { isEdit: false }): void {
        dialogRefSpy = { close: vi.fn() };

        TestBed.configureTestingModule({
            imports: [ProductSaveSuccessDialogComponent, TranslateModule.forRoot()],
            providers: [
                provideNoopAnimations(),
                { provide: MatDialogRef, useValue: dialogRefSpy },
                { provide: MAT_DIALOG_DATA, useValue: data },
            ],
        });

        fixture = TestBed.createComponent(ProductSaveSuccessDialogComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    }

    it('should create', () => {
        createComponent();
        expect(component).toBeTruthy();
    });

    it('should return correct title for create', () => {
        createComponent({ isEdit: false });
        expect(component['titleKey']()).toBe('PRODUCT_DETAIL.CREATE_SUCCESS');
    });

    it('should return correct title for edit', () => {
        createComponent({ isEdit: true });
        expect(component['titleKey']()).toBe('PRODUCT_DETAIL.EDIT_SUCCESS');
    });

    it('should close with action', () => {
        createComponent();
        component.close('Home');
        expect(dialogRefSpy.close).toHaveBeenCalledWith('Home');

        component.close('ProductList');
        expect(dialogRefSpy.close).toHaveBeenCalledWith('ProductList');
    });
});
