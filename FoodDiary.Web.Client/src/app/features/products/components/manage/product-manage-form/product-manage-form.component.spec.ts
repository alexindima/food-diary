import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { of, Subject } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { NavigationService } from '../../../../../services/navigation.service';
import { UsdaService } from '../../../../usda/api/usda.service';
import type { UsdaFoodDetail } from '../../../../usda/models/usda.data';
import { OpenFoodFactsService } from '../../../api/open-food-facts.service';
import { ProductService } from '../../../api/product.service';
import { ProductManageFacade } from '../../../lib/product-manage.facade';
import { MeasurementUnit, type Product, type ProductSearchSuggestion, ProductType, ProductVisibility } from '../../../models/product.data';
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
const SAME_ID_UPDATED_PRODUCT: Product = {
    ...PRODUCT,
    name: 'Updated same id product',
    caloriesPerBase: 180,
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
const USDA_FDC_ID = 123;
const SECOND_USDA_FDC_ID = 456;
const USDA_PROTEIN_NUTRIENT_ID = 1003;
const USDA_SUGGESTION: ProductSearchSuggestion = {
    source: 'usda',
    name: 'USDA suggestion',
    usdaFdcId: USDA_FDC_ID,
};
const SECOND_USDA_SUGGESTION: ProductSearchSuggestion = {
    source: 'usda',
    name: 'Second USDA suggestion',
    usdaFdcId: SECOND_USDA_FDC_ID,
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
    navigationService: {
        navigateToProductListAsync: ReturnType<typeof vi.fn>;
    };
    openFoodFactsService: {
        searchByBarcode: ReturnType<typeof vi.fn>;
    };
    usdaService: UsdaServiceMock;
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

describe('ProductManageFormComponent product inputs', () => {
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

    it('should repopulate the form when the product input is refreshed with the same id', async () => {
        const { component, fixture } = await setupComponentAsync();

        fixture.componentRef.setInput('product', PRODUCT);
        fixture.detectChanges();

        fixture.componentRef.setInput('product', SAME_ID_UPDATED_PRODUCT);
        fixture.detectChanges();

        expect(component.productForm.controls.name.value).toBe(SAME_ID_UPDATED_PRODUCT.name);
        expect(component.productForm.controls.caloriesPerBase.value).toBe(SAME_ID_UPDATED_PRODUCT.caloriesPerBase);
    });

    it('should not apply add prefill over an edit product', async () => {
        const { component, fixture } = await setupComponentAsync();

        fixture.componentRef.setInput('product', PRODUCT);
        fixture.componentRef.setInput('prefill', { barcode: OFF_PRODUCT.barcode, offProduct: OFF_PRODUCT });
        fixture.detectChanges();

        expect(component.productForm.controls.name.value).toBe(PRODUCT.name);
        expect(component.productForm.controls.barcode.value).toBe(PRODUCT.barcode);
    });
});

describe('ProductManageFormComponent prefill behavior', () => {
    it('should ignore stale Open Food Facts lookup response when barcode changes', async () => {
        const { component, fixture, openFoodFactsService } = await setupComponentAsync();
        const lookupResult$ = new Subject<typeof OFF_PRODUCT | null>();
        openFoodFactsService.searchByBarcode.mockReturnValue(lookupResult$);

        fixture.componentRef.setInput('prefill', { barcode: OFF_PRODUCT.barcode });
        fixture.detectChanges();
        component.productForm.controls.barcode.setValue('changed-barcode');
        lookupResult$.next(OFF_PRODUCT);

        expect(component.productForm.controls.name.value).toBe('');
        expect(component.productForm.controls.brand.value).toBeNull();
    });
});

describe('ProductManageFormComponent USDA behavior', () => {
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

    it('should ignore stale USDA detail response when a later USDA suggestion is selected', async () => {
        const { component, usdaService } = await setupComponentAsync();
        const firstDetail$ = new Subject<UsdaFoodDetail | null>();
        const secondDetail$ = new Subject<UsdaFoodDetail | null>();
        usdaService.getFoodDetail.mockImplementation((fdcId: number) => (fdcId === USDA_FDC_ID ? firstDetail$ : secondDetail$));

        component.onNameSuggestionSelected(USDA_SUGGESTION);
        component.onNameSuggestionSelected(SECOND_USDA_SUGGESTION);
        firstDetail$.next(createUsdaFoodDetail(USDA_FDC_ID, 'Old USDA detail'));
        secondDetail$.next(createUsdaFoodDetail(SECOND_USDA_FDC_ID, 'Fresh USDA detail'));

        expect(component.productForm.controls.name.value).toBe('Fresh USDA detail');
        expect(component.productForm.controls.usdaFdcId.value).toBe(SECOND_USDA_FDC_ID);
    });
});

describe('ProductManageFormComponent submit and cancel behavior', () => {
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

    it('should emit saved product after successful submit', async () => {
        const { component, productManageFacade } = await setupComponentAsync();
        let savedProduct: Product | null = null;
        component.saved.subscribe(product => {
            savedProduct = product;
        });
        productManageFacade.submitProductAsync.mockResolvedValue({ product: PRODUCT, error: null });
        component.productForm.patchValue({
            name: 'Valid product',
            caloriesPerBase: 100,
            proteinsPerBase: 10,
        });

        const result = await component.onSubmitAsync();

        expect(result).toBe(PRODUCT);
        expect(savedProduct).toBe(PRODUCT);
    });

    it('should emit cancel without navigation or discard confirmation when cancel mode is emit', async () => {
        const { component, fixture, productManageFacade, navigationService } = await setupComponentAsync();
        let wasCancelled = false;
        component.cancelled.subscribe(() => {
            wasCancelled = true;
        });
        fixture.componentRef.setInput('cancelMode', 'emit');
        fixture.detectChanges();
        component.productForm.markAsDirty();

        await component.onCancelAsync();

        expect(wasCancelled).toBe(true);
        expect(productManageFacade.confirmDiscardChangesAsync).not.toHaveBeenCalled();
        expect(navigationService.navigateToProductListAsync).not.toHaveBeenCalled();
    });
});

async function setupComponentAsync(): Promise<ProductManageFormSetup> {
    const productManageFacade = createProductManageFacadeMock();
    const usdaService = createUsdaServiceMock();
    const navigationService = {
        navigateToProductListAsync: vi.fn().mockResolvedValue(undefined),
    };
    const openFoodFactsService = {
        searchByBarcode: vi.fn().mockReturnValue(of(null)),
    };

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
                useValue: openFoodFactsService,
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
                useValue: navigationService,
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
        navigationService,
        openFoodFactsService,
        usdaService,
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
        getFoodDetail: vi.fn().mockReturnValue(of(createUsdaFoodDetail(USDA_FDC_ID, 'USDA detail'))),
        linkProduct: vi.fn().mockReturnValue(of(null)),
        unlinkProduct: vi.fn().mockReturnValue(of(null)),
    };
}

function createUsdaFoodDetail(fdcId: number, description: string): UsdaFoodDetail {
    return {
        fdcId,
        description,
        foodCategory: null,
        portions: [],
        healthScores: null,
        nutrients: [
            {
                nutrientId: USDA_PROTEIN_NUTRIENT_ID,
                name: 'Protein',
                unit: 'g',
                amountPer100g: USDA_PROTEIN_PER_100G,
                dailyValue: null,
                percentDailyValue: null,
            },
        ],
    };
}
