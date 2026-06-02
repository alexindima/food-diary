import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { of, Subject } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { NavigationService } from '../../../../../services/navigation.service';
import type { UsdaFoodDetail } from '../../../../usda/models/usda.data';
import { ProductService } from '../../../api/product.service';
import { ProductExternalFoodFacade } from '../../../lib/manage/product-external-food.facade';
import { ProductManageFacade } from '../../../lib/product-manage.facade';
import { MeasurementUnit, type Product, type ProductSearchSuggestion, ProductType, ProductVisibility } from '../../../models/product.data';
import { ProductManageFormComponent } from './product-manage-form';

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
const PORTION_AMOUNT = 50;
const BASE_CALORIES = 100;
const PORTION_CALORIES = 50;

type ProductManageFacadeMock = {
    ensurePremiumAccess: ReturnType<typeof vi.fn>;
    confirmDiscardChangesAsync: ReturnType<typeof vi.fn>;
    deleteProductAsync: ReturnType<typeof vi.fn>;
    submitProductAsync: ReturnType<typeof vi.fn>;
};

type ProductExternalFoodFacadeMock = {
    searchByBarcode: ReturnType<typeof vi.fn>;
    getUsdaFoodDetail: ReturnType<typeof vi.fn>;
    linkUsdaProduct: ReturnType<typeof vi.fn>;
    unlinkUsdaProduct: ReturnType<typeof vi.fn>;
};

type ProductManageFormSetup = {
    fixture: ComponentFixture<ProductManageFormComponent>;
    component: ProductManageFormComponent;
    dialogService: {
        open: ReturnType<typeof vi.fn>;
    };
    productManageFacade: ProductManageFacadeMock;
    navigationService: {
        navigateToProductListAsync: ReturnType<typeof vi.fn>;
    };
    externalFoodFacade: ProductExternalFoodFacadeMock;
};

describe('ProductManageFormComponent header state', () => {
    it('should use create title and submit label when product is not provided', async () => {
        const { component } = await setupComponentAsync();

        expect(component['manageHeaderState']()).toEqual({
            titleKey: 'PRODUCT_MANAGE.ADD_TITLE',
            submitIcon: 'add',
            submitLabelKey: 'PRODUCT_MANAGE.ADD_BUTTON',
        });
    });

    it('should use edit title and submit label when product is provided', async () => {
        const { component, fixture } = await setupComponentAsync();

        fixture.componentRef.setInput('product', PRODUCT);

        expect(component['manageHeaderState']()).toEqual({
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
        expect(component['productForm'].controls.name.value).toBe(PRODUCT.name);

        fixture.componentRef.setInput('product', SECOND_PRODUCT);
        fixture.detectChanges();

        expect(component['productForm'].controls.name.value).toBe(SECOND_PRODUCT.name);
        expect(component['productForm'].controls.caloriesPerBase.value).toBe(SECOND_PRODUCT.caloriesPerBase);
    });

    it('should repopulate the form when the product input is refreshed with the same id', async () => {
        const { component, fixture } = await setupComponentAsync();

        fixture.componentRef.setInput('product', PRODUCT);
        fixture.detectChanges();

        fixture.componentRef.setInput('product', SAME_ID_UPDATED_PRODUCT);
        fixture.detectChanges();

        expect(component['productForm'].controls.name.value).toBe(SAME_ID_UPDATED_PRODUCT.name);
        expect(component['productForm'].controls.caloriesPerBase.value).toBe(SAME_ID_UPDATED_PRODUCT.caloriesPerBase);
    });

    it('should not apply add prefill over an edit product', async () => {
        const { component, fixture } = await setupComponentAsync();

        fixture.componentRef.setInput('product', PRODUCT);
        fixture.componentRef.setInput('prefill', { barcode: OFF_PRODUCT.barcode, offProduct: OFF_PRODUCT });
        fixture.detectChanges();

        expect(component['productForm'].controls.name.value).toBe(PRODUCT.name);
        expect(component['productForm'].controls.barcode.value).toBe(PRODUCT.barcode);
    });
});

describe('ProductManageFormComponent prefill behavior', () => {
    it('should ignore stale Open Food Facts lookup response when barcode changes', async () => {
        const { component, fixture, externalFoodFacade } = await setupComponentAsync();
        const lookupResult$ = new Subject<typeof OFF_PRODUCT | null>();
        externalFoodFacade.searchByBarcode.mockReturnValue(lookupResult$);

        fixture.componentRef.setInput('prefill', { barcode: OFF_PRODUCT.barcode });
        fixture.detectChanges();
        component['productForm'].controls.barcode.setValue('changed-barcode');
        lookupResult$.next(OFF_PRODUCT);

        expect(component['productForm'].controls.name.value).toBe('');
        expect(component['productForm'].controls.brand.value).toBeNull();
    });

    it('should apply barcode scanner result and look up Open Food Facts', async () => {
        const { component, dialogService, externalFoodFacade } = await setupComponentAsync();
        dialogService.open.mockReturnValueOnce({ afterClosed: () => of(OFF_PRODUCT.barcode) });

        component['openBarcodeScanner']();

        expect(component['productForm'].controls.barcode.value).toBe(OFF_PRODUCT.barcode);
        expect(externalFoodFacade.searchByBarcode).toHaveBeenCalledWith(OFF_PRODUCT.barcode);
    });
});

describe('ProductManageFormComponent USDA behavior', () => {
    it('should clear stale nutrition values before applying USDA detail', async () => {
        const { component } = await setupComponentAsync();

        component['productForm'].patchValue({
            caloriesPerBase: 500,
            proteinsPerBase: 50,
            fatsPerBase: 20,
            carbsPerBase: 80,
            fiberPerBase: 10,
            alcoholPerBase: 3,
        });

        component['onNameSuggestionSelected'](USDA_SUGGESTION);

        expect(component['productForm'].controls.name.value).toBe('USDA detail');
        expect(component['productForm'].controls.caloriesPerBase.value).toBeNull();
        expect(component['productForm'].controls.proteinsPerBase.value).toBe(EXPECTED_ROUNDED_USDA_PROTEIN);
        expect(component['productForm'].controls.fatsPerBase.value).toBeNull();
        expect(component['productForm'].controls.carbsPerBase.value).toBeNull();
        expect(component['productForm'].controls.fiberPerBase.value).toBeNull();
        expect(component['productForm'].controls.alcoholPerBase.value).toBeNull();
    });

    it('should ignore stale USDA detail response when a later USDA suggestion is selected', async () => {
        const { component, externalFoodFacade } = await setupComponentAsync();
        const firstDetail$ = new Subject<UsdaFoodDetail | null>();
        const secondDetail$ = new Subject<UsdaFoodDetail | null>();
        externalFoodFacade.getUsdaFoodDetail.mockImplementation((fdcId: number) => (fdcId === USDA_FDC_ID ? firstDetail$ : secondDetail$));

        component['onNameSuggestionSelected'](USDA_SUGGESTION);
        component['onNameSuggestionSelected'](SECOND_USDA_SUGGESTION);
        firstDetail$.next(createUsdaFoodDetail(USDA_FDC_ID, 'Old USDA detail'));
        secondDetail$.next(createUsdaFoodDetail(SECOND_USDA_FDC_ID, 'Fresh USDA detail'));

        expect(component['productForm'].controls.name.value).toBe('Fresh USDA detail');
        expect(component['productForm'].controls.usdaFdcId.value).toBe(SECOND_USDA_FDC_ID);
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
        fillValidProductForm(component);

        const firstSubmit = component['onSubmitAsync']();
        const secondSubmit = await component['onSubmitAsync']();

        expect(secondSubmit).toBeNull();
        expect(productManageFacade.submitProductAsync).toHaveBeenCalledTimes(1);

        resolveSubmit({ product: PRODUCT, error: null });
        await firstSubmit;
    });

    it('should emit saved product after successful submit', async () => {
        const { component, productManageFacade } = await setupComponentAsync();
        let savedProduct: Product | null = null;
        component['saved'].subscribe(product => {
            savedProduct = product;
        });
        productManageFacade.submitProductAsync.mockResolvedValue({ product: PRODUCT, error: null });
        fillValidProductForm(component);

        const result = await component['onSubmitAsync']();

        expect(result).toBe(PRODUCT);
        expect(savedProduct).toBe(PRODUCT);
    });

    it('should emit saved product and show USDA warning when post-save sync fails', async () => {
        const { component, productManageFacade } = await setupComponentAsync();
        let savedProduct: Product | null = null;
        component['saved'].subscribe(product => {
            savedProduct = product;
        });
        productManageFacade.submitProductAsync.mockResolvedValue({
            product: PRODUCT,
            error: new HttpErrorResponse({ status: HttpStatusCode.ServiceUnavailable }),
        });
        fillValidProductForm(component);

        const result = await component['onSubmitAsync']();

        expect(result).toBe(PRODUCT);
        expect(savedProduct).toBe(PRODUCT);
        expect(component['globalError']()).toBe('PRODUCT_MANAGE.USDA_SYNC_ERROR');
    });

    it('should skip submit confirmation in dialog mode', async () => {
        const { component, fixture, productManageFacade } = await setupComponentAsync();
        fixture.componentRef.setInput('mode', 'dialog');
        fixture.detectChanges();
        fillValidProductForm(component);

        await component['onSubmitAsync']();

        expect(productManageFacade.submitProductAsync).toHaveBeenCalledWith(null, expect.any(Object), true, expect.any(Function));
    });
});

describe('ProductManageFormComponent cancel delete and nutrition behavior', () => {
    it('should emit cancel without navigation or discard confirmation in dialog mode', async () => {
        const { component, fixture, productManageFacade, navigationService } = await setupComponentAsync();
        let wasCancelled = false;
        component['cancelled'].subscribe(() => {
            wasCancelled = true;
        });
        fixture.componentRef.setInput('mode', 'dialog');
        fixture.detectChanges();
        component['productForm'].markAsDirty();

        await component['onCancelAsync']();

        expect(wasCancelled).toBe(true);
        expect(productManageFacade.confirmDiscardChangesAsync).not.toHaveBeenCalled();
        expect(navigationService.navigateToProductListAsync).not.toHaveBeenCalled();
    });

    it('should stay on page when dirty cancel confirmation is rejected', async () => {
        const { component, productManageFacade, navigationService } = await setupComponentAsync();
        productManageFacade.confirmDiscardChangesAsync.mockResolvedValueOnce(false);
        component['productForm'].markAsDirty();

        await component['onCancelAsync']();

        expect(productManageFacade.confirmDiscardChangesAsync).toHaveBeenCalledOnce();
        expect(navigationService.navigateToProductListAsync).not.toHaveBeenCalled();
    });

    it('should show delete error when owned product delete fails', async () => {
        const { component, fixture, productManageFacade } = await setupComponentAsync();
        fixture.componentRef.setInput('product', PRODUCT);
        fixture.detectChanges();
        productManageFacade.deleteProductAsync.mockResolvedValueOnce('error');

        await component['onDeleteProductAsync']();

        expect(productManageFacade.deleteProductAsync).toHaveBeenCalledOnce();
        expect(component['globalError']()).toBe('PRODUCT_MANAGE.DELETE_ERROR');
        expect(component['isDeleting']()).toBe(false);
    });

    it('should convert nutrition values when switching between base and portion modes', async () => {
        const { component } = await setupComponentAsync();
        component['productForm'].patchValue({
            baseAmount: 100,
            defaultPortionAmount: PORTION_AMOUNT,
            caloriesPerBase: BASE_CALORIES,
        });

        component['onNutritionModeChange']('portion');
        expect(component['nutritionMode']).toBe('portion');
        expect(component['productForm'].controls.caloriesPerBase.value).toBe(PORTION_CALORIES);

        component['onNutritionModeChange']('base');
        expect(component['nutritionMode']).toBe('base');
        expect(component['productForm'].controls.caloriesPerBase.value).toBe(BASE_CALORIES);
    });
});

async function setupComponentAsync(): Promise<ProductManageFormSetup> {
    const productManageFacade = createProductManageFacadeMock();
    const externalFoodFacade = createProductExternalFoodFacadeMock();
    const navigationService = {
        navigateToProductListAsync: vi.fn().mockResolvedValue(void 0),
    };
    const dialogService = {
        open: vi.fn().mockReturnValue({ afterClosed: () => of(null) }),
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
            { provide: ProductExternalFoodFacade, useValue: externalFoodFacade },
            {
                provide: FdUiDialogService,
                useValue: dialogService,
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
        dialogService,
        productManageFacade,
        navigationService,
        externalFoodFacade,
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

function createProductExternalFoodFacadeMock(): ProductExternalFoodFacadeMock {
    return {
        searchByBarcode: vi.fn().mockReturnValue(of(null)),
        getUsdaFoodDetail: vi.fn().mockReturnValue(of(createUsdaFoodDetail(USDA_FDC_ID, 'USDA detail'))),
        linkUsdaProduct: vi.fn().mockReturnValue(of(null)),
        unlinkUsdaProduct: vi.fn().mockReturnValue(of(null)),
    };
}

function fillValidProductForm(component: ProductManageFormComponent): void {
    component['productForm'].patchValue({
        name: 'Valid product',
        caloriesPerBase: 100,
        proteinsPerBase: 10,
    });
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
