import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { NavigationService } from '../../../../services/navigation.service';
import { UsdaService } from '../../../usda/api/usda.service';
import { OpenFoodFactsService } from '../../api/open-food-facts.service';
import { ProductService } from '../../api/product.service';
import { ProductManageFacade } from '../../lib/product-manage.facade';
import { MeasurementUnit, type Product, type ProductSearchSuggestion, ProductType, ProductVisibility } from '../../models/product.data';
import { ProductManageFormComponent } from './product-manage-form.component';

const PRODUCT: Product = {
    id: 'product-1',
    name: 'Test product',
    barcode: null,
    brand: null,
    productType: ProductType.Unknown,
    category: null,
    description: null,
    comment: null,
    imageUrl: null,
    imageAssetId: null,
    baseUnit: MeasurementUnit.G,
    baseAmount: 100,
    defaultPortionAmount: 100,
    caloriesPerBase: 100,
    proteinsPerBase: 10,
    fatsPerBase: 5,
    carbsPerBase: 12,
    fiberPerBase: 1,
    alcoholPerBase: 0,
    usageCount: 0,
    visibility: ProductVisibility.Private,
    createdAt: new Date('2026-01-01T00:00:00Z'),
    isOwnedByCurrentUser: true,
    qualityScore: 80,
    qualityGrade: 'green',
};
const SECOND_PRODUCT: Product = {
    ...PRODUCT,
    id: 'product-2',
    name: 'Second product',
    caloriesPerBase: 240,
    proteinsPerBase: 20,
};
const OFF_PRODUCT = {
    barcode: '4600000000000',
    name: 'Open Food Facts product',
    caloriesPer100G: 310,
    proteinsPer100G: 9,
    fatsPer100G: 7,
    carbsPer100G: 42,
    fiberPer100G: 3,
    brand: 'OFF brand',
};
const USDA_SUGGESTION: ProductSearchSuggestion = {
    source: 'usda',
    name: 'USDA suggestion',
    usdaFdcId: 123,
};
const USDA_PROTEIN_PER_100G = 12.345;
const EXPECTED_ROUNDED_USDA_PROTEIN = 12.3;

type ProductManageFacadeMock = {
    ensurePremiumAccess: ReturnType<typeof vi.fn>;
    confirmDiscardChangesAsync: ReturnType<typeof vi.fn>;
    deleteProductAsync: ReturnType<typeof vi.fn>;
    submitProductAsync: ReturnType<typeof vi.fn>;
};

type UsdaServiceMock = {
    getFoodDetail: ReturnType<typeof vi.fn>;
    linkProduct: ReturnType<typeof vi.fn>;
    unlinkProduct: ReturnType<typeof vi.fn>;
};

type ProductManageFormSetup = {
    fixture: ComponentFixture<ProductManageFormComponent>;
    component: ProductManageFormComponent;
    productManageFacade: ProductManageFacadeMock;
};

describe('ProductManageFormComponent header state', () => {
    it('should use create title and submit label when product is not provided', async () => {
        const { component } = await setupComponentAsync();

        expect(component.manageHeaderState()).toEqual({
            titleKey: 'PRODUCT_MANAGE.ADD_TITLE',
            submitIcon: 'add',
            submitLabelKey: 'PRODUCT_MANAGE.ADD_BUTTON',
        });
    });

    it('should use edit title and submit label when product is provided', async () => {
        const { component, fixture } = await setupComponentAsync();

        fixture.componentRef.setInput('product', PRODUCT);

        expect(component.manageHeaderState()).toEqual({
            titleKey: 'PRODUCT_MANAGE.EDIT_TITLE',
            submitIcon: 'save',
            submitLabelKey: 'PRODUCT_MANAGE.SAVE_BUTTON',
        });
    });
});

describe('ProductManageFormComponent form behavior', () => {
    it('should repopulate the form when the product input changes', async () => {
        const { component, fixture } = await setupComponentAsync();

        fixture.componentRef.setInput('product', PRODUCT);
        fixture.detectChanges();
        expect(component.productForm.controls.name.value).toBe(PRODUCT.name);

        fixture.componentRef.setInput('product', SECOND_PRODUCT);
        fixture.detectChanges();

        expect(component.productForm.controls.name.value).toBe(SECOND_PRODUCT.name);
        expect(component.productForm.controls.caloriesPerBase.value).toBe(SECOND_PRODUCT.caloriesPerBase);
    });

    it('should not apply add prefill over an edit product', async () => {
        const { component, fixture } = await setupComponentAsync();

        fixture.componentRef.setInput('product', PRODUCT);
        fixture.componentRef.setInput('prefill', { barcode: OFF_PRODUCT.barcode, offProduct: OFF_PRODUCT });
        fixture.detectChanges();

        expect(component.productForm.controls.name.value).toBe(PRODUCT.name);
        expect(component.productForm.controls.barcode.value).toBe(PRODUCT.barcode);
    });

    it('should clear stale nutrition values before applying USDA detail', async () => {
        const { component } = await setupComponentAsync();

        component.productForm.patchValue({
            caloriesPerBase: 500,
            proteinsPerBase: 50,
            fatsPerBase: 20,
            carbsPerBase: 80,
            fiberPerBase: 10,
            alcoholPerBase: 3,
        });

        component.onNameSuggestionSelected(USDA_SUGGESTION);

        expect(component.productForm.controls.name.value).toBe('USDA detail');
        expect(component.productForm.controls.caloriesPerBase.value).toBeNull();
        expect(component.productForm.controls.proteinsPerBase.value).toBe(EXPECTED_ROUNDED_USDA_PROTEIN);
        expect(component.productForm.controls.fatsPerBase.value).toBeNull();
        expect(component.productForm.controls.carbsPerBase.value).toBeNull();
        expect(component.productForm.controls.fiberPerBase.value).toBeNull();
        expect(component.productForm.controls.alcoholPerBase.value).toBeNull();
    });

    it('should ignore duplicate submit while a save is already in progress', async () => {
        const { component, productManageFacade } = await setupComponentAsync();
        let resolveSubmit!: (value: { product: Product | null; error: null }) => void;
        productManageFacade.submitProductAsync.mockReturnValue(
            new Promise(resolve => {
                resolveSubmit = resolve;
            }),
        );
        component.productForm.patchValue({
            name: 'Valid product',
            caloriesPerBase: 100,
            proteinsPerBase: 10,
        });

        const firstSubmit = component.onSubmitAsync();
        const secondSubmit = await component.onSubmitAsync();

        expect(secondSubmit).toBeNull();
        expect(productManageFacade.submitProductAsync).toHaveBeenCalledTimes(1);

        resolveSubmit({ product: PRODUCT, error: null });
        await firstSubmit;
    });
});

async function setupComponentAsync(): Promise<ProductManageFormSetup> {
    const productManageFacade = createProductManageFacadeMock();
    const usdaService = createUsdaServiceMock();

    await TestBed.configureTestingModule({
        imports: [ProductManageFormComponent, TranslateModule.forRoot()],
        providers: [
            {
                provide: ProductService,
                useValue: {
                    searchSuggestions: vi.fn().mockReturnValue(of([])),
                },
            },
            {
                provide: OpenFoodFactsService,
                useValue: {
                    searchByBarcode: vi.fn().mockReturnValue(of(null)),
                },
            },
            {
                provide: UsdaService,
                useValue: usdaService,
            },
            {
                provide: FdUiDialogService,
                useValue: {
                    open: vi.fn().mockReturnValue({ afterClosed: () => of(null) }),
                },
            },
            {
                provide: NavigationService,
                useValue: {
                    navigateToProductListAsync: vi.fn().mockResolvedValue(undefined),
                },
            },
            {
                provide: ProductManageFacade,
                useValue: productManageFacade,
            },
        ],
    }).compileComponents();

    const fixture = TestBed.createComponent(ProductManageFormComponent);
    return {
        fixture,
        component: fixture.componentInstance,
        productManageFacade,
    };
}

function createProductManageFacadeMock(): ProductManageFacadeMock {
    return {
        ensurePremiumAccess: vi.fn().mockReturnValue(true),
        confirmDiscardChangesAsync: vi.fn().mockResolvedValue(true),
        deleteProductAsync: vi.fn().mockResolvedValue('deleted'),
        submitProductAsync: vi.fn().mockResolvedValue({ product: null, error: null }),
    };
}

function createUsdaServiceMock(): UsdaServiceMock {
    return {
        getFoodDetail: vi.fn().mockReturnValue(
            of({
                fdcId: 123,
                description: 'USDA detail',
                foodCategory: null,
                portions: [],
                healthScores: null,
                nutrients: [
                    {
                        nutrientId: 1003,
                        name: 'Protein',
                        unit: 'g',
                        amountPer100g: USDA_PROTEIN_PER_100G,
                        dailyValue: null,
                        percentDailyValue: null,
                    },
                ],
            }),
        ),
        linkProduct: vi.fn().mockReturnValue(of(null)),
        unlinkProduct: vi.fn().mockReturnValue(of(null)),
    };
}
