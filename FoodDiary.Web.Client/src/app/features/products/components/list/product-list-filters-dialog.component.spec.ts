import { TestBed } from '@angular/core/testing';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import {
    ProductListFiltersDialogComponent,
    ProductListFiltersDialogData,
} from './product-list-filters-dialog.component';
import { ProductType } from '../../models/product.data';

describe('ProductListFiltersDialogComponent', () => {
    let component: ProductListFiltersDialogComponent;
    let dialogRefSpy: jasmine.SpyObj<MatDialogRef<ProductListFiltersDialogComponent>>;

    const defaultData: ProductListFiltersDialogData = {
        onlyMine: false,
        productTypes: [ProductType.Meat, ProductType.Fruit],
    };

    function createComponent(data: ProductListFiltersDialogData = defaultData): void {
        dialogRefSpy = jasmine.createSpyObj('MatDialogRef', ['close']);

        TestBed.configureTestingModule({
            imports: [ProductListFiltersDialogComponent, TranslateModule.forRoot()],
            providers: [
                provideNoopAnimations(),
                { provide: MatDialogRef, useValue: dialogRefSpy },
                { provide: MAT_DIALOG_DATA, useValue: data },
            ],
        });

        const fixture = TestBed.createComponent(ProductListFiltersDialogComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    }

    it('should create', () => {
        createComponent();
        expect(component).toBeTruthy();
    });

    it('should initialize with provided filter data', () => {
        createComponent({ onlyMine: true, productTypes: [ProductType.Dairy] });

        expect(component.visibilityValue).toBe('mine');
        expect(component.isTypeSelected(ProductType.Dairy)).toBeTrue();
        expect(component.isTypeSelected(ProductType.Meat)).toBeFalse();
    });

    it('should toggle product type selection', () => {
        createComponent();

        expect(component.isTypeSelected(ProductType.Meat)).toBeTrue();

        component.toggleType(ProductType.Meat);
        expect(component.isTypeSelected(ProductType.Meat)).toBeFalse();

        component.toggleType(ProductType.Grain);
        expect(component.isTypeSelected(ProductType.Grain)).toBeTrue();
    });

    it('should apply filters on submit', () => {
        createComponent({ onlyMine: false, productTypes: [] });

        component.onVisibilityChange('mine');
        component.toggleType(ProductType.Seafood);
        component.onApply();

        expect(dialogRefSpy.close).toHaveBeenCalledWith(
            jasmine.objectContaining({
                onlyMine: true,
                productTypes: jasmine.arrayContaining([ProductType.Seafood]),
            }),
        );
    });

    it('should cancel without applying', () => {
        createComponent();

        component.onCancel();

        expect(dialogRefSpy.close).toHaveBeenCalledWith(null);
    });
});
