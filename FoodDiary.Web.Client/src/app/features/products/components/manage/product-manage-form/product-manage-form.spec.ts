import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import type { Provider } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { FdTourService } from 'fd-tour';
import { type FdUiDialogConfig, FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import type {
    FdUiImagePreviewDialogComponent,
    FdUiImagePreviewDialogData,
} from 'fd-ui-kit/image-preview-dialog/fd-ui-image-preview-dialog';
import { firstValueFrom, of, Subject, throwError } from 'rxjs';
import { describe, expect, it, onTestFinished, vi } from 'vitest';

import { stubScrollIntoView } from '../../../../../../testing/dom-testing';
import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import { ImageGalleryEditorComponent } from '../../../../../components/shared/image-gallery-editor/image-gallery-editor';
import { NAME_SEARCH_DEBOUNCE_MS } from '../../../../../config/runtime-ui.tokens';
import { NavigationService } from '../../../../../services/navigation.service';
import { DEVELOPMENT_SCREENS, type DevelopmentScreen } from '../../../../../shared/development/development-tools';
import { ImageUploadFacade } from '../../../../../shared/lib/image-upload.facade';
import { DEFAULT_NUTRITION_BASE_AMOUNT } from '../../../../../shared/lib/nutrition.constants';
import { USDA_NUTRIENT_IDS } from '../../../../../shared/lib/usda-nutrient.constants';
import type { FoodVisionResponse } from '../../../../../shared/models/ai.data';
import { type ImageSelection, imageSelection } from '../../../../../shared/models/image-upload.data';
import {
    MeasurementUnit,
    type Product,
    type ProductSearchSuggestion,
    ProductType,
    ProductVisibility,
} from '../../../../../shared/models/product.data';
import { entityId } from '../../../../../shared/models/semantics/entity-id';
import { usdaFoodId } from '../../../../../shared/models/semantics/usda-food-id';
import type { UsdaFoodDetail } from '../../../../../shared/models/usda.data';
import { ProductService } from '../../../api/product.service';
import type { ProductAiRecognitionDialogComponent } from '../../../dialogs/product-ai-recognition-dialog/product-ai-recognition-dialog';
import type {
    ProductAiDialogData,
    ProductAiRecognitionResult,
} from '../../../dialogs/product-ai-recognition-dialog/product-ai-recognition-dialog.types';
import { ProductExternalFoodFacade } from '../../../lib/manage/product-external-food.facade';
import { ProductAiRecognitionFacade } from '../../../lib/product-ai-recognition.facade';
import {
    PRODUCT_BRAND_MAX_LENGTH,
    PRODUCT_MAX_PHOTOS,
    PRODUCT_MAX_PIECE_AMOUNT,
    PRODUCT_MAX_PIECE_CALORIES_PER_BASE,
    PRODUCT_MAX_PIECE_NUTRIENT_PER_BASE,
    PRODUCT_MAX_WEIGHT_OR_VOLUME_AMOUNT,
    PRODUCT_MAX_WEIGHT_OR_VOLUME_CALORIES_PER_BASE,
    PRODUCT_MAX_WEIGHT_OR_VOLUME_NUTRIENT_PER_BASE,
    PRODUCT_NAME_MAX_LENGTH,
} from '../../../lib/product-manage.constants';
import { ProductManageFacade } from '../../../lib/product-manage.facade';
import type { ProductDeleteResult } from '../../../lib/product-manage.types';
import { ProductBasicInfoComponent } from '../product-basic-info/product-basic-info';
import type { ProductFormValues } from '../product-manage-lib/product-manage-form.types';
import { ProductManageFormComponent } from './product-manage-form';

const PRODUCT: Product = {
    id: entityId<'product'>('product-1'),
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
    id: entityId<'product'>('product-2'),
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
const MILK_CALORIES_PER_BASE = 60;
const MILK_LARGE_PORTION_AMOUNT = 500;
const MILK_ORIGINAL_PORTION_CALORIES = 150;

describe('Product editor pending save', () => {
    it.each([false, true])('locks all editable controls until save settles, failure=%s', async fail => {
        const { component, fixture, productManageFacade } = await setupComponentAsync();
        fillValidProductForm(component);
        component['productForm']().markAsDirty();
        let settle: (() => void) | undefined;
        productManageFacade.submitProductAsync.mockReturnValue(
            new Promise(resolve => {
                settle = (): void => {
                    resolve(fail ? { product: null, error: new HttpErrorResponse({ status: 503 }) } : { product: PRODUCT, error: null });
                };
            }),
        );
        const draft = component['productFormModel']();
        const saving = component['onSubmitAsync']();
        fixture.detectChanges();

        const element = fixture.nativeElement as HTMLElement;
        const controls = [...element.querySelectorAll('form input, form textarea, form select, form button')];
        expect(controls.length).toBeGreaterThan(0);
        expect(controls.every(control => control.matches(':disabled'))).toBe(true);
        expect(element.querySelector('fd-page-header')?.hasAttribute('inert')).toBe(true);

        settle?.();
        await saving;
        fixture.detectChanges();
        expect(controls.some(control => !control.matches(':disabled'))).toBe(true);
        if (fail) {
            expect(component['productFormModel']()).toEqual(draft);
            expect(component['hasUnsavedChanges']()).toBe(true);
        }
    });

    it.each([
        [HttpStatusCode.Unauthorized, 'FORM_ERRORS.UNAUTHORIZED'],
        [HttpStatusCode.BadRequest, 'FORM_ERRORS.INVALID_DATA'],
    ])('shows the save error for HTTP %s and clears it after editing', async (status, errorKey) => {
        const { el, fixture, productManageFacade } = await setupComponentAsync();
        fixture.componentRef.setInput('product', PRODUCT);
        productManageFacade.submitProductAsync.mockResolvedValueOnce({ product: null, error: new HttpErrorResponse({ status }) });
        fixture.detectChanges();

        el.querySelector<HTMLButtonElement>('button[type="submit"]')?.click();
        await fixture.whenStable();
        fixture.detectChanges();

        expect(productManageFacade.submitProductAsync).toHaveBeenCalledOnce();
        expect(el.querySelector('.product-manage__footer-error')?.textContent.trim()).toBe(errorKey);

        const name = el.querySelector<HTMLInputElement>('fd-ui-autocomplete input');
        if (name === null) {
            throw new Error('Missing product name input');
        }
        name.value = 'Retry product';
        name.dispatchEvent(new Event('input', { bubbles: true }));
        fixture.detectChanges();

        expect(el.querySelector('.product-manage__footer-error')).toBeNull();
    });
});

type ProductManageFacadeMock = {
    committed$: Subject<void>;
    ensurePremiumAccess: ReturnType<typeof vi.fn>;
    confirmDiscardChangesAsync: ReturnType<typeof vi.fn>;
    deleteProductAsync: ReturnType<typeof vi.fn>;
    submitProductAsync: ReturnType<typeof vi.fn<ProductManageFacade['submitProductAsync']>>;
};

type ProductExternalFoodFacadeMock = {
    searchByBarcode: ReturnType<typeof vi.fn>;
    getUsdaFoodDetail: ReturnType<typeof vi.fn>;
    linkUsdaProduct: ReturnType<typeof vi.fn>;
    unlinkUsdaProduct: ReturnType<typeof vi.fn>;
};

type ProductManageFormSetup = {
    fixture: ComponentFixture<ProductManageFormComponent>;
    el: HTMLElement;
    component: ProductManageFormComponent;
    dialogService: {
        open: ReturnType<typeof vi.fn>;
    };
    productManageFacade: ProductManageFacadeMock;
    uploadImage: ReturnType<typeof vi.fn<ImageUploadFacade['upload']>>;
    navigationService: {
        navigateToProductListAsync: ReturnType<typeof vi.fn<NavigationService['navigateToProductListAsync']>>;
    };
    tourService: {
        start: ReturnType<typeof vi.fn>;
    };
    externalFoodFacade: ProductExternalFoodFacadeMock;
    searchSuggestions: ReturnType<typeof vi.fn<ProductService['searchSuggestions']>>;
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

    it('should start product form tour from the header action', async () => {
        const { component, el, fixture, tourService } = await setupComponentAsync();

        fixture.detectChanges();
        const tourButton = el.querySelector('[data-tour-id="product-manage-tour-help"]');

        component['startProductManageTour']();

        expect(tourButton).not.toBeNull();
        expect(tourService.start).toHaveBeenCalledWith(
            expect.objectContaining({
                id: 'product-manage',
            }),
            { force: true },
        );
    });
});

describe('ProductManageFormComponent product inputs', () => {
    it('should repopulate the form when the product input changes', async () => {
        const { component, fixture } = await setupComponentAsync();

        fixture.componentRef.setInput('product', PRODUCT);
        fixture.detectChanges();
        expect(productValues(component).name).toBe(PRODUCT.name);

        fixture.componentRef.setInput('product', SECOND_PRODUCT);
        fixture.detectChanges();

        expect(productValues(component).name).toBe(SECOND_PRODUCT.name);
        expect(productValues(component).caloriesPerBase).toBe(SECOND_PRODUCT.caloriesPerBase);
    });

    it('should repopulate the form when the product input is refreshed with the same id', async () => {
        const { component, fixture } = await setupComponentAsync();

        fixture.componentRef.setInput('product', PRODUCT);
        fixture.detectChanges();

        fixture.componentRef.setInput('product', SAME_ID_UPDATED_PRODUCT);
        fixture.detectChanges();

        expect(productValues(component).name).toBe(SAME_ID_UPDATED_PRODUCT.name);
        expect(productValues(component).caloriesPerBase).toBe(SAME_ID_UPDATED_PRODUCT.caloriesPerBase);
    });

    it('should not apply add prefill over an edit product', async () => {
        const { component, fixture } = await setupComponentAsync();

        fixture.componentRef.setInput('product', PRODUCT);
        fixture.componentRef.setInput('prefill', { barcode: OFF_PRODUCT.barcode, offProduct: OFF_PRODUCT });
        fixture.detectChanges();

        expect(productValues(component).name).toBe(PRODUCT.name);
        expect(productValues(component).barcode).toBe(PRODUCT.barcode);
    });
});

describe('ProductManageFormComponent prefill behavior', () => {
    it('should ignore stale Open Food Facts lookup response when barcode changes', async () => {
        const { component, fixture, externalFoodFacade } = await setupComponentAsync();
        const lookupResult$ = new Subject<typeof OFF_PRODUCT | null>();
        externalFoodFacade.searchByBarcode.mockReturnValue(lookupResult$);

        fixture.componentRef.setInput('prefill', { barcode: OFF_PRODUCT.barcode });
        fixture.detectChanges();
        component['productForm'].barcode().value.set('changed-barcode');
        lookupResult$.next(OFF_PRODUCT);

        expect(productValues(component).name).toBe('');
        expect(productValues(component).brand).toBeNull();
    });

    it('should not mark the form dirty when applying route barcode prefill', async () => {
        const { component, fixture } = await setupComponentAsync();

        fixture.componentRef.setInput('prefill', { barcode: OFF_PRODUCT.barcode });
        fixture.detectChanges();

        expect(productValues(component).barcode).toBe(OFF_PRODUCT.barcode);
        expect(component['productForm']().dirty()).toBe(false);
    });

    it('should apply barcode scanner result and look up Open Food Facts', async () => {
        const { component, dialogService, externalFoodFacade } = await setupComponentAsync();
        dialogService.open.mockReturnValueOnce({ afterClosed: () => of(OFF_PRODUCT.barcode) });

        component['openBarcodeScanner']();
        TestBed.tick();

        expect(productValues(component).barcode).toBe(OFF_PRODUCT.barcode);
        expect(component['productForm'].barcode().dirty()).toBe(true);
        expect(externalFoodFacade.searchByBarcode).toHaveBeenCalledWith(OFF_PRODUCT.barcode);
    });
});

describe('ProductManageFormComponent USDA behavior', () => {
    it('should clear stale nutrition values before applying USDA detail', async () => {
        const { component } = await setupComponentAsync();

        patchProductForm(component, {
            caloriesPerBase: 500,
            proteinsPerBase: 50,
            fatsPerBase: 20,
            carbsPerBase: 80,
            fiberPerBase: 10,
            alcoholPerBase: 3,
        });

        component['onNameSuggestionSelected'](USDA_SUGGESTION);
        TestBed.tick();

        expect(productValues(component).name).toBe('USDA detail');
        expect(productValues(component).caloriesPerBase).toBeNull();
        expect(productValues(component).proteinsPerBase).toBe(EXPECTED_ROUNDED_USDA_PROTEIN);
        expect(productValues(component).fatsPerBase).toBeNull();
        expect(productValues(component).carbsPerBase).toBeNull();
        expect(productValues(component).fiberPerBase).toBeNull();
        expect(productValues(component).alcoholPerBase).toBeNull();
    });

    it('should ignore stale USDA detail response when a later USDA suggestion is selected', async () => {
        const { component, externalFoodFacade } = await setupComponentAsync();
        const firstDetail$ = new Subject<UsdaFoodDetail | null>();
        const secondDetail$ = new Subject<UsdaFoodDetail | null>();
        externalFoodFacade.getUsdaFoodDetail.mockImplementation((fdcId: number) => (fdcId === USDA_FDC_ID ? firstDetail$ : secondDetail$));

        component['onNameSuggestionSelected'](USDA_SUGGESTION);
        TestBed.tick();
        component['onNameSuggestionSelected'](SECOND_USDA_SUGGESTION);
        TestBed.tick();
        firstDetail$.next(createUsdaFoodDetail(USDA_FDC_ID, 'Old USDA detail'));
        secondDetail$.next(createUsdaFoodDetail(SECOND_USDA_FDC_ID, 'Fresh USDA detail'));

        expect(productValues(component).name).toBe('Fresh USDA detail');
        expect(productValues(component).usdaFdcId).toBe(SECOND_USDA_FDC_ID);
    });
});

describe('ProductManageFormComponent submit and cancel behavior', () => {
    it('should prevent native form submit when saving', async () => {
        const { component, el, fixture, productManageFacade } = await setupComponentAsync();
        productManageFacade.submitProductAsync.mockResolvedValue({ product: PRODUCT, error: null });
        fillValidProductForm(component);
        fixture.detectChanges();

        const form = el.querySelector('form');
        expect(form).not.toBeNull();

        const submitEvent = new Event('submit', { bubbles: true, cancelable: true });
        const wasNotCancelled = form?.dispatchEvent(submitEvent);
        await fixture.whenStable();

        expect(wasNotCancelled).toBe(false);
        expect(submitEvent.defaultPrevented).toBe(true);
        expect(productManageFacade.submitProductAsync).toHaveBeenCalledOnce();
    });

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

    it('should emit saved product after successful submit in dialog mode', async () => {
        const { component, fixture, productManageFacade } = await setupComponentAsync();
        let savedProduct: Product | null = null;
        component['saved'].subscribe(product => {
            savedProduct = product;
        });
        fixture.componentRef.setInput('mode', 'dialog');
        fixture.detectChanges();
        productManageFacade.submitProductAsync.mockResolvedValue({ product: PRODUCT, error: null });
        fillValidProductForm(component);

        const result = await component['onSubmitAsync']();

        expect(result).toBe(PRODUCT);
        expect(savedProduct).toBe(PRODUCT);
    });

    it('should emit saved product and show USDA warning in dialog mode when post-save sync fails', async () => {
        const { component, fixture, productManageFacade } = await setupComponentAsync();
        let savedProduct: Product | null = null;
        component['saved'].subscribe(product => {
            savedProduct = product;
        });
        fixture.componentRef.setInput('mode', 'dialog');
        fixture.detectChanges();
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

    it('should skip post-save redirect in dialog mode', async () => {
        const { component, fixture, productManageFacade } = await setupComponentAsync();
        fixture.componentRef.setInput('mode', 'dialog');
        fixture.detectChanges();
        fillValidProductForm(component);

        await component['onSubmitAsync']();

        expect(productManageFacade.submitProductAsync).toHaveBeenCalledWith(null, expect.any(Object), true, expect.any(Function));
    });
});

describe('ProductManageFormComponent stored source synchronization', () => {
    it('clears a committed source after retry saves a different source in create mode', async () => {
        const { component, productManageFacade, externalFoodFacade } = await setupComponentAsync();
        const savedProduct = { ...PRODUCT, usdaFdcId: USDA_FDC_ID };
        productManageFacade.submitProductAsync.mockImplementation(
            async (...args: Parameters<ProductManageFacade['submitProductAsync']>) => {
                await args[3]?.(savedProduct);
                return { product: savedProduct, error: null };
            },
        );
        fillValidProductForm(component);

        await component['onSubmitAsync']();

        expect(externalFoodFacade.unlinkUsdaProduct).toHaveBeenCalledWith(savedProduct.id);
        expect(externalFoodFacade.linkUsdaProduct).not.toHaveBeenCalled();
    });

    it('does not relink a source that the returned saved product already contains', async () => {
        const { component, productManageFacade, externalFoodFacade } = await setupComponentAsync();
        const savedProduct = { ...PRODUCT, usdaFdcId: USDA_FDC_ID };
        productManageFacade.submitProductAsync.mockImplementation(
            async (...args: Parameters<ProductManageFacade['submitProductAsync']>) => {
                await args[3]?.(savedProduct);
                return { product: savedProduct, error: null };
            },
        );
        fillValidProductForm(component);
        patchProductForm(component, { usdaFdcId: USDA_FDC_ID });

        await component['onSubmitAsync']();

        expect(externalFoodFacade.linkUsdaProduct).not.toHaveBeenCalled();
        expect(externalFoodFacade.unlinkUsdaProduct).not.toHaveBeenCalled();
    });

    it('links a selected source when the saved product is still unlinked', async () => {
        const { component, productManageFacade, externalFoodFacade } = await setupComponentAsync();
        productManageFacade.submitProductAsync.mockImplementation(
            async (...args: Parameters<ProductManageFacade['submitProductAsync']>) => {
                await args[3]?.({ ...PRODUCT, usdaFdcId: null });
                return { product: PRODUCT, error: null };
            },
        );
        fillValidProductForm(component);
        patchProductForm(component, { usdaFdcId: USDA_FDC_ID });

        await component['onSubmitAsync']();

        expect(externalFoodFacade.linkUsdaProduct).toHaveBeenCalledWith(PRODUCT.id, USDA_FDC_ID);
        expect(externalFoodFacade.unlinkUsdaProduct).not.toHaveBeenCalled();
    });
});

describe('ProductManageFormComponent page-mode output lifecycle', () => {
    it('should not emit saved product after submit navigates away', async () => {
        const { component, productManageFacade } = await setupComponentAsync();
        const savedSpy = vi.fn();
        component['saved'].subscribe(savedSpy);
        productManageFacade.submitProductAsync.mockResolvedValue({ product: PRODUCT, error: null });
        fillValidProductForm(component);

        const result = await component['onSubmitAsync']();

        expect(result).toBe(PRODUCT);
        expect(savedSpy).not.toHaveBeenCalled();
    });
});

describe('ProductManageFormComponent validation limits', () => {
    it('keeps a whitespace-only name invalid before sending a product request', async () => {
        const { component, productManageFacade } = await setupComponentAsync();
        fillValidProductForm(component);
        patchProductForm(component, { name: ' \t\n ' });

        expect(component['productForm']().invalid()).toBe(true);
        expect(await component['onSubmitAsync']()).toBeNull();
        expect(productManageFacade.submitProductAsync).not.toHaveBeenCalled();
    });

    it('should not submit product values that exceed product limits', async () => {
        const { component, productManageFacade } = await setupComponentAsync();
        fillValidProductForm(component);
        patchProductForm(component, {
            name: 'x'.repeat(PRODUCT_NAME_MAX_LENGTH + 1),
            defaultPortionAmount: PRODUCT_MAX_WEIGHT_OR_VOLUME_AMOUNT + 1,
        });

        const result = await component['onSubmitAsync']();

        expect(result).toBeNull();
        expect(productManageFacade.submitProductAsync).not.toHaveBeenCalled();
    });

    it('should apply stricter piece amount limit when product unit is pieces', async () => {
        const { component, productManageFacade } = await setupComponentAsync();
        fillValidProductForm(component);
        patchProductForm(component, {
            baseUnit: MeasurementUnit.PCS,
            baseAmount: 1,
            defaultPortionAmount: PRODUCT_MAX_PIECE_AMOUNT + 1,
        });

        const result = await component['onSubmitAsync']();

        expect(result).toBeNull();
        expect(productManageFacade.submitProductAsync).not.toHaveBeenCalled();
    });

    it('should not submit nutrition values that exceed gram base limits', async () => {
        const { component, productManageFacade } = await setupComponentAsync();
        fillValidProductForm(component);
        patchProductForm(component, {
            caloriesPerBase: PRODUCT_MAX_WEIGHT_OR_VOLUME_CALORIES_PER_BASE + 1,
            proteinsPerBase: PRODUCT_MAX_WEIGHT_OR_VOLUME_NUTRIENT_PER_BASE + 1,
        });

        const result = await component['onSubmitAsync']();

        expect(result).toBeNull();
        expect(productManageFacade.submitProductAsync).not.toHaveBeenCalled();
    });

    it('should apply wider nutrition limits when product unit is pieces', async () => {
        const { component, productManageFacade } = await setupComponentAsync();
        fillValidProductForm(component);
        patchProductForm(component, {
            baseUnit: MeasurementUnit.PCS,
            baseAmount: 1,
            defaultPortionAmount: 1,
            caloriesPerBase: PRODUCT_MAX_PIECE_CALORIES_PER_BASE + 1,
            proteinsPerBase: PRODUCT_MAX_PIECE_NUTRIENT_PER_BASE + 1,
        });

        const result = await component['onSubmitAsync']();

        expect(result).toBeNull();
        expect(productManageFacade.submitProductAsync).not.toHaveBeenCalled();
    });

    it('should allow editing but not submit portion nutrition values above the current portion range', async () => {
        const { component, productManageFacade } = await setupComponentAsync();
        fillValidProductForm(component);
        patchProductForm(component, {
            defaultPortionAmount: 250,
            caloriesPerBase: PRODUCT_MAX_WEIGHT_OR_VOLUME_CALORIES_PER_BASE,
            proteinsPerBase: PRODUCT_MAX_WEIGHT_OR_VOLUME_NUTRIENT_PER_BASE,
        });
        component['onNutritionModeChange']('portion');
        patchProductForm(component, {
            caloriesPerBase: 2501,
            proteinsPerBase: 251,
        });

        expect(component['productForm']().invalid()).toBe(false);

        const result = await component['onSubmitAsync']();

        expect(result).toBeNull();
        expect(productManageFacade.submitProductAsync).not.toHaveBeenCalled();
    });
});

describe('ProductManageFormComponent cancel delete and nutrition behavior', () => {
    it('should stay on page when dirty cancel confirmation is rejected', async () => {
        const { component, productManageFacade, navigationService } = await setupComponentAsync();
        productManageFacade.confirmDiscardChangesAsync.mockResolvedValueOnce(false);
        component['productForm']().markAsDirty();

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
        patchProductForm(component, {
            baseAmount: 100,
            defaultPortionAmount: PORTION_AMOUNT,
            caloriesPerBase: BASE_CALORIES,
        });

        component['onNutritionModeChange']('portion');
        expect(component['nutritionMode']).toBe('portion');
        expect(productValues(component).caloriesPerBase).toBe(PORTION_CALORIES);

        component['onNutritionModeChange']('base');
        expect(component['nutritionMode']).toBe('base');
        expect(productValues(component).caloriesPerBase).toBe(BASE_CALORIES);
    });

    it('preserves nutrients after changing display basis and saves the original base values', async () => {
        const { component, productManageFacade } = await setupComponentAsync();
        const baseProteins = 12.5;
        const portionProteins = 6.25;
        patchProductForm(component, { name: 'Oats', defaultPortionAmount: 50, caloriesPerBase: 350, proteinsPerBase: baseProteins });
        component['onNutritionModeChange']('portion');
        expect(productValues(component).proteinsPerBase).toBe(portionProteins);
        await component['onSubmitAsync']();
        expect(productManageFacade.submitProductAsync).toHaveBeenCalledWith(
            null,
            expect.objectContaining({ proteinsPerBase: baseProteins, caloriesPerBase: 350 }),
            expect.any(Boolean),
            expect.any(Function),
        );
        component['onNutritionModeChange']('base');
        expect(productValues(component).proteinsPerBase).toBe(baseProteins);
    });
});

describe('Product invalid portion mode transitions', () => {
    it('preserves base nutrition when leaving portion mode before an invalid quantity is restored', async () => {
        const { component, productManageFacade } = await setupComponentAsync();
        patchProductForm(component, { name: 'Milk', defaultPortionAmount: 250, caloriesPerBase: 60 });
        component['onNutritionModeChange']('portion');
        component['productForm'].defaultPortionAmount().value.set(0);
        component['onNutritionModeChange']('base');
        expect(productValues(component).caloriesPerBase).toBe(MILK_CALORIES_PER_BASE);
        component['productForm'].defaultPortionAmount().value.set(MILK_LARGE_PORTION_AMOUNT);
        await component['onSubmitAsync']();
        expect(productManageFacade.submitProductAsync.mock.calls[0][1]).toMatchObject({ caloriesPerBase: 60, defaultPortionAmount: 500 });
    });

    it('rescales base nutrition when a valid amount is restored after entering portion mode with zero', async () => {
        const { component, fixture, productManageFacade } = await setupComponentAsync();
        patchProductForm(component, { name: 'Milk', defaultPortionAmount: 0, caloriesPerBase: 60 });
        component['onNutritionModeChange']('portion');
        component['productForm'].defaultPortionAmount().value.set(MILK_LARGE_PORTION_AMOUNT);
        fixture.detectChanges();
        expect(productValues(component)).toMatchObject({ caloriesPerBase: 300 });
        await component['onSubmitAsync']();
        expect(productManageFacade.submitProductAsync.mock.calls[0][1]).toMatchObject({ caloriesPerBase: 60, defaultPortionAmount: 500 });
    });
});

describe('Product portion edits', () => {
    it('rescales the displayed portion and saves the same per-base nutrition when default quantity changes', async () => {
        const { component, fixture, productManageFacade } = await setupComponentAsync();
        fixture.componentRef.setInput('product', {
            ...PRODUCT,
            baseUnit: MeasurementUnit.ML,
            defaultPortionAmount: 250,
            caloriesPerBase: 60,
            proteinsPerBase: 3,
            fatsPerBase: 2,
            carbsPerBase: 7,
        });
        fixture.detectChanges();
        component['onNutritionModeChange']('portion');
        component['productForm'].defaultPortionAmount().value.set(MILK_LARGE_PORTION_AMOUNT);
        fixture.detectChanges();
        expect(productValues(component)).toMatchObject({ caloriesPerBase: 300, proteinsPerBase: 15, fatsPerBase: 10, carbsPerBase: 35 });
        await component['onSubmitAsync']();
        expect(productManageFacade.submitProductAsync.mock.calls[0][1]).toMatchObject({
            defaultPortionAmount: 500,
            caloriesPerBase: 60,
            proteinsPerBase: 3,
            fatsPerBase: 2,
            carbsPerBase: 7,
        });
    });

    it('retains the last valid display basis through an invalid zero quantity and an immediate mode switch', async () => {
        const { component, fixture } = await setupComponentAsync();
        patchProductForm(component, { name: 'Milk', defaultPortionAmount: 250, caloriesPerBase: 60 });
        component['onNutritionModeChange']('portion');
        component['productForm'].defaultPortionAmount().value.set(0);
        fixture.detectChanges();
        component['productForm'].defaultPortionAmount().value.set(MILK_LARGE_PORTION_AMOUNT);
        component['onNutritionModeChange']('base');
        expect(productValues(component).caloriesPerBase).toBe(MILK_CALORIES_PER_BASE);
    });

    it('adopts recognized portion values without rescaling them from the previous product basis', async () => {
        const { component, fixture, productManageFacade } = await setupComponentAsync();
        patchProductForm(component, { name: 'Old product', defaultPortionAmount: 100, caloriesPerBase: 100 });
        component['onNutritionModeChange']('portion');
        component['applyAiResult']({
            name: 'Recognized milk',
            image: null,
            baseUnit: MeasurementUnit.ML,
            baseAmount: 250,
            caloriesPerBase: 150,
            proteinsPerBase: 7.5,
            fatsPerBase: 5,
            carbsPerBase: 17.5,
            fiberPerBase: 0,
            alcoholPerBase: 0,
        });
        fixture.detectChanges();
        expect(productValues(component).caloriesPerBase).toBe(MILK_ORIGINAL_PORTION_CALORIES);
        component['productForm'].defaultPortionAmount().value.set(MILK_LARGE_PORTION_AMOUNT);
        await component['onSubmitAsync']();
        expect(productManageFacade.submitProductAsync.mock.calls[0][1]).toMatchObject({ defaultPortionAmount: 500, caloriesPerBase: 60 });
    });
});

describe('Product recognition integration', () => {
    it.each([false, true])('keeps the product unchanged when recognition is unavailable or canceled, premium=%s', async premium => {
        const { el, fixture, dialogService, productManageFacade } = await setupComponentAsync();
        productManageFacade.ensurePremiumAccess.mockReturnValue(premium);
        fixture.componentRef.setInput('product', PRODUCT);
        fixture.detectChanges();

        const recognize = el.querySelector<HTMLButtonElement>('.product-manage__ai button');
        expect(recognize).not.toBeNull();
        recognize?.click();
        fixture.detectChanges();

        expect(dialogService.open).toHaveBeenCalledTimes(premium ? 1 : 0);
        expect(el.querySelector<HTMLInputElement>('fd-ui-autocomplete input')?.value).toBe(PRODUCT.name);
        expect(el.querySelector('.product-manage__footer-error')).toBeNull();
    });

    it('keeps the photo-limit warning visible after applying recognized values to a full gallery', async () => {
        const { el, fixture, dialogService } = await setupComponentAsync();
        const gallery = Array.from({ length: PRODUCT_MAX_PHOTOS }, (_, index) => ({
            imageAssetId: `original-asset-${index}`,
            imageUrl: `https://images.example.test/original-${index}.jpg`,
        }));
        fixture.componentRef.setInput('product', { ...PRODUCT, images: gallery });
        fixture.detectChanges();
        const response = new Subject<ProductAiRecognitionResult>();
        dialogService.open.mockReturnValueOnce({ afterClosed: (): Subject<ProductAiRecognitionResult> => response });
        const recognize = el.querySelector<HTMLButtonElement>('.product-manage__ai button');
        if (recognize === null) {
            throw new Error('Missing product recognition button');
        }
        recognize.click();
        const recognized = imageSelection('https://images.example.test/new.jpg', 'recognized-asset');
        response.next({
            name: 'Recognized product',
            image: recognized,
            images: [recognized],
            baseUnit: MeasurementUnit.G,
            baseAmount: PORTION_AMOUNT,
            caloriesPerBase: PORTION_CALORIES,
            proteinsPerBase: 1,
            fatsPerBase: 0,
            carbsPerBase: 0,
            fiberPerBase: 0,
            alcoholPerBase: 0,
        });
        response.complete();
        fixture.detectChanges();

        expect(el.querySelector<HTMLInputElement>('fd-ui-autocomplete input')?.value).toBe('Recognized product');
        expect(el.querySelector('img[src*="original-0.jpg"]')).not.toBeNull();
        expect(el.querySelector('img[src*="new.jpg"]')).toBeNull();
        expect(el.querySelector('.product-manage__footer-error')?.textContent.trim()).toBe('PRODUCT_MANAGE.PHOTO_IMPORT_LIMIT');

        fixture.componentRef.setInput('product', SECOND_PRODUCT);
        fixture.detectChanges();

        expect(el.querySelector<HTMLInputElement>('fd-ui-autocomplete input')?.value).toBe(SECOND_PRODUCT.name);
        expect(el.querySelector('.product-manage__footer-error')).toBeNull();
    });

    it('applies recognized portion values and asks before discarding them', async () => {
        const { component, fixture, productManageFacade } = await setupComponentAsync();
        fixture.detectChanges();
        component['applyAiResult']({
            name: 'Recognized product',
            image: null,
            baseUnit: MeasurementUnit.G,
            baseAmount: PORTION_AMOUNT,
            caloriesPerBase: PORTION_CALORIES,
            proteinsPerBase: 1,
            fatsPerBase: 0,
            carbsPerBase: 0,
            fiberPerBase: 0,
            alcoholPerBase: 0,
        });
        expect(component['nutritionMode']).toBe('portion');
        expect(productValues(component).defaultPortionAmount).toBe(PORTION_AMOUNT);
        expect(productValues(component).caloriesPerBase).toBe(PORTION_CALORIES);
        await component['onCancelAsync']();
        expect(productManageFacade.confirmDiscardChangesAsync).toHaveBeenCalledOnce();
    });
});

describe('Product creation numeric validation', () => {
    it('allows an explicitly zero calorie product with empty optional nutrients', async () => {
        const { component, productManageFacade } = await setupComponentAsync();
        patchProductForm(component, { name: 'Water', caloriesPerBase: 0 });
        await component['onSubmitAsync']();
        expect(productManageFacade.submitProductAsync).toHaveBeenCalledOnce();
    });

    it.each([
        'caloriesPerBase',
        'proteinsPerBase',
        'fatsPerBase',
        'carbsPerBase',
        'fiberPerBase',
        'alcoholPerBase',
        'defaultPortionAmount',
    ] as const)('blocks non-finite %s instead of silently saving zero', async field => {
        const { component, productManageFacade } = await setupComponentAsync();
        fillValidProductForm(component);
        patchProductForm(component, { [field]: Number.NaN });
        await component['onSubmitAsync']();
        expect(productManageFacade.submitProductAsync).not.toHaveBeenCalled();
    });

    it('still requires an explicit calorie value', async () => {
        const { component, productManageFacade } = await setupComponentAsync();
        patchProductForm(component, { name: 'Water' });
        await component['onSubmitAsync']();
        expect(productManageFacade.submitProductAsync).not.toHaveBeenCalled();
    });
});

describe('Product editor route departure', () => {
    it('keeps edits after stay and reuses one pending decision for repeated navigation', async () => {
        const { component, productManageFacade } = await setupComponentAsync();
        component['productForm']().markAsDirty();
        let resolveDecision: (value: boolean) => void = () => {};
        productManageFacade.confirmDiscardChangesAsync.mockReturnValue(
            new Promise<boolean>(resolve => {
                resolveDecision = resolve;
            }),
        );
        const first = component.canLeaveAsync();
        const repeated = component.canLeaveAsync();
        expect(productManageFacade.confirmDiscardChangesAsync).toHaveBeenCalledOnce();
        resolveDecision(false);
        expect(await first).toBe(false);
        expect(await repeated).toBe(false);
        expect(component['productForm']().dirty()).toBe(true);
    });

    it('blocks departure during save and permits the committed redirect without another confirmation', async () => {
        const { component, productManageFacade } = await setupComponentAsync();
        component['productForm']().markAsDirty();
        component['isSubmitting'].set(true);
        expect(await component.canLeaveAsync()).toBe(false);
        productManageFacade.committed$.next();
        expect(await component.canLeaveAsync()).toBe(true);
        expect(productManageFacade.confirmDiscardChangesAsync).not.toHaveBeenCalled();
    });

    it('asks only once for Cancel and restores protection if navigation fails', async () => {
        const { component, productManageFacade, navigationService } = await setupComponentAsync();
        component['productForm']().markAsDirty();
        navigationService.navigateToProductListAsync.mockImplementation(async () =>
            component.canLeaveAsync().then(allowed => {
                expect(allowed).toBe(true);
                throw new Error('Navigation failed');
            }),
        );
        await expect(component['onCancelAsync']()).rejects.toThrow('Navigation failed');
        expect(productManageFacade.confirmDiscardChangesAsync).toHaveBeenCalledOnce();
        productManageFacade.confirmDiscardChangesAsync.mockResolvedValue(false);
        expect(await component.canLeaveAsync()).toBe(false);
        expect(productManageFacade.confirmDiscardChangesAsync).toHaveBeenCalledTimes(2);
    });
});

describe('product development capability', () => {
    it('fills the live private form, refuses replacement, clears and never saves', async () => {
        const screens: DevelopmentScreen[] = [];
        const dispose = vi.fn();
        const { component, fixture, productManageFacade } = await setupComponentAsync({
            attach: screen => {
                screens.push(screen);
                return dispose;
            },
        });
        const screen = screens[0];
        screen.fillDraft?.({ name: 'Draft apple', baseUnit: 'G', defaultPortionAmount: 150, caloriesPerBase: 52 });
        fixture.detectChanges();
        expect(component['productFormModel']().name).toBe('Draft apple');
        expect(component['productFormModel']().baseAmount).toBe(PRODUCT.baseAmount);
        expect(component['productFormModel']().visibility).toBe(ProductVisibility.Private);
        expect(screen.isDirty?.()).toBe(true);
        expect(() => screen.fillDraft?.({})).toThrow('Unsaved');
        expect(productManageFacade.submitProductAsync).not.toHaveBeenCalled();
        screen.clearDraft?.();
        expect(component['productFormModel']().name).toBe('');
        expect(screen.isDirty?.()).toBe(false);
        fixture.destroy();
        expect(dispose).toHaveBeenCalledOnce();
    });
});

describe('Product editor capability snapshot', () => {
    it('reports native validation and refuses draft changes during a pending submission', async () => {
        const screens: DevelopmentScreen[] = [];
        const editor = await setupComponentAsync({
            attach: screen => {
                screens.push(screen);
                return (): void => undefined;
            },
        });
        editor.fixture.detectChanges();
        const screen = screens[0];
        const emptyDraft = screen.read();
        expect(emptyDraft).toMatchObject({
            isNew: true,
            id: null,
            valid: false,
            loading: false,
        });
        expect(emptyDraft).toHaveProperty('validation.name', expect.arrayContaining(['required']));
        expect(emptyDraft).toHaveProperty('validation.calories', expect.arrayContaining(['required']));
        enterProductValue(requiredProductInput(editor.el, 'fd-ui-autocomplete input'), 'Fresh apple');
        enterProductValue(requiredProductInput(editor.el, '.nutrition-editor__input--calories input'), String(BASE_CALORIES));
        editor.fixture.detectChanges();
        expect(screen.read()).toMatchObject({
            isNew: true,
            valid: true,
            loading: false,
            values: { name: 'Fresh apple', caloriesPerBase: BASE_CALORIES, visibility: ProductVisibility.Private },
            validation: { name: [], calories: [] },
        });
        expect(screen.isDirty?.()).toBe(true);
        let rejectSave: (() => void) | undefined;
        editor.productManageFacade.submitProductAsync.mockReturnValueOnce(
            new Promise(resolve => {
                rejectSave = (): void => {
                    resolve({ product: null, error: new HttpErrorResponse({ status: 503 }) });
                };
            }),
        );
        requiredFormButton(editor.el, 'button[type="submit"]').click();
        await vi.waitFor(() => {
            editor.fixture.detectChanges();
            expect(screen.read()).toMatchObject({ loading: true });
        });
        expect(() => screen.clearDraft?.()).toThrow('A ready NEW product editor is required');
        expect(requiredProductInput(editor.el, 'fd-ui-autocomplete input').value).toBe('Fresh apple');
        expect(editor.productManageFacade.submitProductAsync).toHaveBeenCalledTimes(1);
        rejectSave?.();
        await editor.fixture.whenStable();
        editor.fixture.detectChanges();
        expect(screen.read()).toMatchObject({ loading: false, error: 'PRODUCT_MANAGE.SAVE_ERROR', values: { name: 'Fresh apple' } });
        expect(screen.isDirty?.()).toBe(true);
    });
});

describe('Product editor capability privacy', () => {
    it('reports the existing product identity and omits photo references from the serialized snapshot', async () => {
        const screens: DevelopmentScreen[] = [];
        const editor = await setupComponentAsync({
            attach: screen => {
                screens.push(screen);
                return (): void => undefined;
            },
        });
        const imgUrl = 'https://images.example.test/product-cover.jpg';
        const galleryUrl = 'https://images.example.test/product-gallery.jpg';
        const assetId = 'test-product-photo';
        editor.fixture.componentRef.setInput('product', {
            ...PRODUCT,
            imageUrl: imgUrl,
            images: [{ imageAssetId: assetId, imageUrl: galleryUrl }],
        });
        editor.fixture.detectChanges();
        expect(editor.el.querySelector('fd-image-gallery-editor img[src*="product-gallery.jpg"]')).not.toBeNull();
        const screen = screens[0];
        const snapshot = screen.read();

        expect(snapshot).toMatchObject({ isNew: false, id: PRODUCT.id, valid: true, values: { name: PRODUCT.name } });
        expect(JSON.stringify(snapshot)).not.toContain(imgUrl);
        expect(JSON.stringify(snapshot)).not.toContain(galleryUrl);
        expect(JSON.stringify(snapshot)).not.toContain(assetId);
        expect(() => screen.clearDraft?.()).toThrow('A ready NEW product editor is required');
        expect(screen.isDirty?.()).toBe(false);
    });
});

describe('Product editor gallery departure', () => {
    it('blocks leaving or deleting while a photo uploads and lets the pristine editor leave after failure', async () => {
        const { component, el, fixture, uploadImage, productManageFacade } = await setupComponentAsync();
        const pending = new Subject<ImageSelection>();
        uploadImage.mockReturnValueOnce(pending);
        fixture.componentRef.setInput('product', PRODUCT);
        fixture.detectChanges();
        expect(await component.canLeaveAsync()).toBe(true);

        beginNativeGalleryUpload(el);
        const save = requiredFormButton(el, 'button[type="submit"]');
        const remove = requiredFormButton(el, '.product-manage__delete-button button');
        const recognize = requiredFormButton(el, '.product-manage__ai button');
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(uploadImage).toHaveBeenCalledOnce();
            expect(save.matches(':disabled')).toBe(true);
        });

        expect(await component.canLeaveAsync()).toBe(false);
        expect(remove.matches(':disabled')).toBe(true);
        expect(recognize.disabled).toBe(true);
        remove.click();
        expect(productManageFacade.deleteProductAsync).not.toHaveBeenCalled();

        pending.error(new Error('Upload unavailable'));
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(save.matches(':disabled')).toBe(false);
        });
        expect(remove.matches(':disabled')).toBe(false);
        expect(recognize.disabled).toBe(false);
        expect(await component.canLeaveAsync()).toBe(true);
        expect(productManageFacade.confirmDiscardChangesAsync).not.toHaveBeenCalled();
    });

    it('asks before discarding a successfully uploaded gallery edit', async () => {
        const { component, el, fixture, uploadImage, productManageFacade } = await setupComponentAsync();
        const photo = imageSelection('https://images.example.test/added-photo.jpg', 'added-asset');
        uploadImage.mockReturnValueOnce(of(photo));
        productManageFacade.confirmDiscardChangesAsync.mockResolvedValueOnce(false);
        fixture.componentRef.setInput('product', PRODUCT);
        fixture.detectChanges();

        beginNativeGalleryUpload(el);
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(el.querySelector('fd-image-gallery-editor img[src*="added-photo.jpg"]')).not.toBeNull();
            expect(el.querySelector('button[type="submit"]')?.matches(':disabled')).toBe(false);
        });

        expect(await component.canLeaveAsync()).toBe(false);
        expect(productManageFacade.confirmDiscardChangesAsync).toHaveBeenCalledOnce();
        expect(productManageFacade.submitProductAsync).not.toHaveBeenCalled();
    });
});

describe('Product editor rendered source prefill', () => {
    it('prefers the supplied Open Food Facts entry over a raw barcode and submits a private creation', async () => {
        const { el, fixture, productManageFacade, externalFoodFacade } = await setupComponentAsync();
        fixture.componentRef.setInput('prefill', { barcode: ' 4006381333931 ', offProduct: OFF_PRODUCT });
        fixture.detectChanges();

        expect(el.querySelector<HTMLInputElement>('fd-ui-autocomplete input')?.value).toBe(OFF_PRODUCT.name);
        expect(el.querySelector<HTMLInputElement>('input[aria-label="PRODUCT_MANAGE.BRAND"]')?.value).toBe(OFF_PRODUCT.brand);
        expect(externalFoodFacade.searchByBarcode).not.toHaveBeenCalled();
        expect(productManageFacade.submitProductAsync).not.toHaveBeenCalled();

        productManageFacade.submitProductAsync.mockResolvedValueOnce({ product: PRODUCT, error: null });
        requiredFormButton(el, 'button[type="submit"]').click();
        await fixture.whenStable();

        expect(productManageFacade.submitProductAsync).toHaveBeenCalledOnce();
        const [existing, request] = productManageFacade.submitProductAsync.mock.calls[0];
        expect(existing).toBeNull();
        expect(request).toMatchObject({ name: OFF_PRODUCT.name, barcode: OFF_PRODUCT.barcode, visibility: ProductVisibility.Private });
    });

    it('does not overwrite the editable product with an obsolete barcode lookup', async () => {
        const { el, fixture, externalFoodFacade } = await setupComponentAsync();
        const result = new Subject<typeof OFF_PRODUCT | null>();
        externalFoodFacade.searchByBarcode.mockReturnValueOnce(result);
        fixture.componentRef.setInput('prefill', { barcode: OFF_PRODUCT.barcode });
        fixture.detectChanges();
        expect(externalFoodFacade.searchByBarcode).toHaveBeenCalledExactlyOnceWith(OFF_PRODUCT.barcode);

        fixture.componentRef.setInput('product', SECOND_PRODUCT);
        fixture.detectChanges();
        result.next(OFF_PRODUCT);
        result.complete();
        fixture.detectChanges();

        expect(el.querySelector<HTMLInputElement>('fd-ui-autocomplete input')?.value).toBe(SECOND_PRODUCT.name);
        expect(el.querySelector<HTMLInputElement>('input[aria-label="PRODUCT_MANAGE.BRAND"]')?.value).toBe('');
    });

    it.each([
        [null, OFF_PRODUCT.barcode],
        ['', OFF_PRODUCT.barcode],
        ['4006381333931', '4006381333931'],
    ] as const)('keeps the existing product data when its scanner closes with %j', async (returned, expectedBarcode) => {
        const { el, fixture, dialogService, externalFoodFacade } = await setupComponentAsync();
        fixture.componentRef.setInput('product', { ...PRODUCT, barcode: OFF_PRODUCT.barcode });
        dialogService.open.mockReturnValueOnce({ afterClosed: () => of(returned) });
        fixture.detectChanges();

        requiredFormButton(el, 'button[aria-label="PRODUCT_MANAGE.SCAN_BARCODE"]').click();
        fixture.detectChanges();

        expect(dialogService.open).toHaveBeenCalledOnce();
        expect(el.querySelector<HTMLInputElement>('input[aria-label="PRODUCT_MANAGE.BARCODE"]')?.value).toBe(expectedBarcode);
        expect(el.querySelector<HTMLInputElement>('fd-ui-autocomplete input')?.value).toBe(PRODUCT.name);
        expect(externalFoodFacade.searchByBarcode).not.toHaveBeenCalled();
    });
});

describe('Product editor native deletion controls', () => {
    it('honors only the first action before repaint and restores the editor after deletion is cancelled', async () => {
        const { el, fixture, productManageFacade } = await setupComponentAsync();
        const confirm = new Subject<ProductDeleteResult>();
        const result = firstValueFrom(confirm);
        productManageFacade.deleteProductAsync.mockReturnValueOnce(result);
        fixture.componentRef.setInput('product', PRODUCT);
        fixture.detectChanges();
        const button = requiredFormButton(el, '.product-manage__delete-button button');
        const save = requiredFormButton(el, 'button[type="submit"]');

        button.click();
        button.click();
        save.click();
        fixture.detectChanges();
        expect(button.matches(':disabled')).toBe(true);
        expect(save.matches(':disabled')).toBe(true);
        expect(productManageFacade.deleteProductAsync).toHaveBeenCalledExactlyOnceWith(
            PRODUCT,
            expect.objectContaining({ name: PRODUCT.name }),
        );
        expect(productManageFacade.submitProductAsync).not.toHaveBeenCalled();

        confirm.next('cancelled');
        confirm.complete();
        await result;
        fixture.detectChanges();

        expect(save.matches(':disabled')).toBe(false);
        expect(el.querySelector<HTMLInputElement>('fd-ui-autocomplete input')?.value).toBe(PRODUCT.name);
        expect(el.querySelector('.product-manage__footer-error')).toBeNull();
    });
});

describe('Product editor dialog ownership', () => {
    it('closes the scanner and ignores a late barcode after the editor is destroyed', async () => {
        const { el, fixture, dialogService, externalFoodFacade } = await setupComponentAsync();
        const closed = new Subject<string | null>();
        const close = vi.fn();
        dialogService.open.mockReturnValueOnce({ afterClosed: () => closed, close });
        fixture.detectChanges();
        requiredFormButton(el, 'button[aria-label="PRODUCT_MANAGE.SCAN_BARCODE"]').click();
        expect(closed.observed).toBe(true);

        fixture.destroy();

        expect(close).toHaveBeenCalledOnce();
        expect(closed.observed).toBe(false);
        closed.next(OFF_PRODUCT.barcode);
        closed.complete();
        expect(externalFoodFacade.searchByBarcode).not.toHaveBeenCalled();
    });

    it('closes the pending photo review when its product editor is destroyed', async () => {
        const { el, fixture, dialogService, productManageFacade } = await setupComponentAsync();
        const closed = new Subject<ProductAiRecognitionResult | null>();
        const close = vi.fn();
        dialogService.open.mockReturnValueOnce({ afterClosed: () => closed, close });
        fixture.detectChanges();
        requiredFormButton(el, '.product-manage__ai button').click();
        expect(closed.observed).toBe(true);

        fixture.destroy();

        expect(close).toHaveBeenCalledOnce();
        expect(closed.observed).toBe(false);
        expect(productManageFacade.submitProductAsync).not.toHaveBeenCalled();
    });
});

describe('Product editor rendered suggestion ownership', () => {
    it('keeps a custom name typed during USDA lookup while displaying the returned nutrients', async () => {
        const editor = await setupComponentAsync();
        const result = new Subject<UsdaFoodDetail>();
        editor.externalFoodFacade.getUsdaFoodDetail.mockReturnValueOnce(result);
        await chooseProductSuggestionAsync(editor, USDA_SUGGESTION);
        const name = requiredProductInput(editor.el, 'fd-ui-autocomplete input');
        expect(name.value).toBe(USDA_SUGGESTION.name);
        expect(editor.externalFoodFacade.getUsdaFoodDetail).toHaveBeenCalledExactlyOnceWith(USDA_FDC_ID);
        const ownName = 'My morning oats';
        enterProductValue(name, ownName);
        editor.fixture.detectChanges();

        result.next(createUsdaFoodDetail(USDA_FDC_ID, 'Provider description'));
        result.complete();
        editor.fixture.detectChanges();

        expect(name.value).toBe(ownName);
        expect(requiredProductInput(editor.el, '.nutrition-editor__input--proteins input').value).toBe(
            String(EXPECTED_ROUNDED_USDA_PROTEIN),
        );
        expect(editor.productManageFacade.submitProductAsync).not.toHaveBeenCalled();
    });

    it('leaves the selected name editable and permits a manual save when the USDA detail request fails', async () => {
        const editor = await setupComponentAsync();
        editor.externalFoodFacade.getUsdaFoodDetail.mockReturnValueOnce(throwError(() => new Error('USDA unavailable')));
        await chooseProductSuggestionAsync(editor, USDA_SUGGESTION);
        expect(requiredProductInput(editor.el, 'fd-ui-autocomplete input').value).toBe(USDA_SUGGESTION.name);
        const calories = requiredProductInput(editor.el, '.nutrition-editor__input--calories input');
        expect(calories.disabled).toBe(false);
        expect(calories.value).toBe('');
        enterProductValue(calories, String(BASE_CALORIES));
        editor.fixture.detectChanges();

        requiredFormButton(editor.el, 'button[type="submit"]').click();
        await editor.fixture.whenStable();

        expect(editor.productManageFacade.submitProductAsync).toHaveBeenCalledExactlyOnceWith(
            null,
            expect.objectContaining({ name: USDA_SUGGESTION.name, caloriesPerBase: BASE_CALORIES }),
            false,
            expect.any(Function),
        );
    });

    it('saves a manually entered product from a USDA suggestion without a source ID', async () => {
        const editor = await setupComponentAsync();
        const suggestion: ProductSearchSuggestion = { source: 'usda', name: 'Rolled oats' };
        const saved = { ...PRODUCT, name: suggestion.name, caloriesPerBase: BASE_CALORIES };
        editor.productManageFacade.submitProductAsync.mockImplementationOnce(async (_product, _request, _skip, afterSave) => {
            await afterSave?.(saved);
            return { product: saved, error: null };
        });
        await chooseProductSuggestionAsync(editor, suggestion);

        expect(requiredProductInput(editor.el, 'fd-ui-autocomplete input').value).toBe(suggestion.name);
        expect(editor.externalFoodFacade.getUsdaFoodDetail).not.toHaveBeenCalled();
        const calories = requiredProductInput(editor.el, '.nutrition-editor__input--calories input');
        expect(calories.value).toBe('');
        enterProductValue(calories, String(BASE_CALORIES));
        editor.fixture.detectChanges();
        requiredFormButton(editor.el, 'button[type="submit"]').click();
        await editor.fixture.whenStable();

        expect(editor.productManageFacade.submitProductAsync).toHaveBeenCalledExactlyOnceWith(
            null,
            expect.objectContaining({ name: suggestion.name, caloriesPerBase: BASE_CALORIES }),
            false,
            expect.any(Function),
        );
        expect(editor.externalFoodFacade.linkUsdaProduct).not.toHaveBeenCalled();
        expect(editor.externalFoodFacade.unlinkUsdaProduct).not.toHaveBeenCalled();
    });
});

describe('Product editor external source handoff', () => {
    it.each([true, false])('keeps the chosen source when recognition is accepted=%s during USDA loading', async accepted => {
        const editor = await setupComponentAsync();
        const detail = new Subject<UsdaFoodDetail>();
        const recognition = new Subject<ProductAiRecognitionResult | null>();
        editor.externalFoodFacade.getUsdaFoodDetail.mockReturnValueOnce(detail);
        editor.dialogService.open.mockReturnValueOnce({ afterClosed: (): Subject<ProductAiRecognitionResult | null> => recognition });
        await chooseProductSuggestionAsync(editor, USDA_SUGGESTION);
        const name = requiredProductInput(editor.el, 'fd-ui-autocomplete input');
        const proteins = requiredProductInput(editor.el, '.nutrition-editor__input--proteins input');
        expect(name.value).toBe(USDA_SUGGESTION.name);
        expect(detail.observed).toBe(true);

        requiredFormButton(editor.el, '.product-manage__ai button').click();
        expect(recognition.observed).toBe(true);
        const recognizedName = 'Recognized packaging';
        const recognizedProteins = 6;
        recognition.next(
            accepted
                ? {
                      name: recognizedName,
                      image: null,
                      baseUnit: MeasurementUnit.G,
                      baseAmount: PORTION_AMOUNT,
                      caloriesPerBase: PORTION_CALORIES,
                      proteinsPerBase: recognizedProteins,
                      fatsPerBase: 0,
                      carbsPerBase: 0,
                      fiberPerBase: 0,
                      alcoholPerBase: 0,
                  }
                : null,
        );
        recognition.complete();
        editor.fixture.detectChanges();
        expect(name.value).toBe(accepted ? recognizedName : USDA_SUGGESTION.name);
        expect(proteins.value).toBe(accepted ? String(recognizedProteins) : '');

        const providerName = 'USDA product details';
        detail.next(createUsdaFoodDetail(USDA_FDC_ID, providerName));
        detail.complete();
        editor.fixture.detectChanges();

        expect(name.value).toBe(accepted ? recognizedName : providerName);
        expect(proteins.value).toBe(String(accepted ? recognizedProteins : EXPECTED_ROUNDED_USDA_PROTEIN));
    });

    it('keeps the chosen Open Food Facts details when a previous USDA response arrives', async () => {
        const editor = await setupComponentAsync();
        const detail = new Subject<UsdaFoodDetail>();
        editor.externalFoodFacade.getUsdaFoodDetail.mockReturnValueOnce(detail);
        await chooseProductSuggestionAsync(editor, USDA_SUGGESTION);
        const selected: ProductSearchSuggestion = { ...OFF_PRODUCT, source: 'openFoodFacts' };
        await chooseProductSuggestionAsync(editor, selected);
        expect(requiredProductInput(editor.el, 'input[aria-label="PRODUCT_MANAGE.BRAND"]').value).toBe(OFF_PRODUCT.brand);

        detail.next(createUsdaFoodDetail(USDA_FDC_ID, 'Prior USDA product'));
        detail.complete();
        editor.fixture.detectChanges();

        expect(requiredProductInput(editor.el, 'fd-ui-autocomplete input').value).toBe(selected.name);
        expect(requiredProductInput(editor.el, '.nutrition-editor__input--proteins input').value).toBe(String(OFF_PRODUCT.proteinsPer100G));
    });
});

describe('Product editor USDA nutrition basis', () => {
    it.each(['before', 'during'])('uses the selected 50 g portion when portion mode is chosen %s the lookup', async when => {
        const editor = await setupComponentAsync();
        const pending = new Subject<UsdaFoodDetail>();
        editor.externalFoodFacade.getUsdaFoodDetail.mockReturnValueOnce(pending);
        editor.fixture.detectChanges();
        const portion = requiredProductInput(editor.el, 'input[aria-label="PRODUCT_MANAGE.DEFAULT_PORTION_AMOUNT"]');
        enterProductValue(portion, String(PORTION_AMOUNT));
        editor.fixture.detectChanges();
        if (when === 'before') {
            selectProductPortionMode(editor);
        }
        await chooseProductSuggestionAsync(editor, USDA_SUGGESTION);
        if (when === 'during') {
            selectProductPortionMode(editor);
        }
        const providerProtein = 20;
        pending.next(createUsdaNutritionDetail(providerProtein));
        pending.complete();
        editor.fixture.detectChanges();

        const portionEnergy = 50;
        const portionProtein = 10;
        expect(requiredProductInput(editor.el, '.nutrition-editor__input--calories input').value).toBe(String(portionEnergy));
        expect(requiredProductInput(editor.el, '.nutrition-editor__input--proteins input').value).toBe(String(portionProtein));
        requiredFormButton(editor.el, 'button[type="submit"]').click();
        await editor.fixture.whenStable();
        expect(editor.productManageFacade.submitProductAsync).toHaveBeenCalledExactlyOnceWith(
            null,
            expect.objectContaining({
                baseAmount: DEFAULT_NUTRITION_BASE_AMOUNT,
                baseUnit: MeasurementUnit.G,
                defaultPortionAmount: PORTION_AMOUNT,
                caloriesPerBase: BASE_CALORIES,
                proteinsPerBase: providerProtein,
            }),
            false,
            expect.any(Function),
        );
    });

    it('keeps a manually entered zero and imports the remaining nutrients on the same gram basis', async () => {
        const editor = await setupComponentAsync();
        const pending = new Subject<UsdaFoodDetail>();
        editor.externalFoodFacade.getUsdaFoodDetail.mockReturnValueOnce(pending);
        await chooseProductSuggestionAsync(editor, USDA_SUGGESTION);
        const proteins = requiredProductInput(editor.el, '.nutrition-editor__input--proteins input');
        expect(proteins.value).toBe('');
        enterProductValue(proteins, '0');
        editor.fixture.detectChanges();

        pending.next(createUsdaNutritionDetail(USDA_PROTEIN_PER_100G));
        pending.complete();
        editor.fixture.detectChanges();

        expect(proteins.value).toBe('0');
        expect(requiredProductInput(editor.el, '.nutrition-editor__input--calories input').value).toBe(String(BASE_CALORIES));
        requiredFormButton(editor.el, 'button[type="submit"]').click();
        await editor.fixture.whenStable();
        expect(editor.productManageFacade.submitProductAsync).toHaveBeenCalledExactlyOnceWith(
            null,
            expect.objectContaining({ baseUnit: MeasurementUnit.G, caloriesPerBase: BASE_CALORIES, proteinsPerBase: 0 }),
            false,
            expect.any(Function),
        );
    });
});

describe('Product editor USDA unit ownership', () => {
    it('keeps the piece unit and manual per-piece values chosen during a gram lookup', async () => {
        const editor = await setupComponentAsync();
        const pending = new Subject<UsdaFoodDetail>();
        editor.externalFoodFacade.getUsdaFoodDetail.mockReturnValueOnce(pending);
        await chooseProductSuggestionAsync(editor, USDA_SUGGESTION);
        const unit = requiredFormButton(editor.el, '.product-manage__serving-row fd-ui-select button');
        unit.click();
        editor.fixture.detectChanges();
        const pieces = Array.from(document.body.querySelectorAll<HTMLButtonElement>('.fd-ui-select__menu button[role="option"]')).find(
            option => option.textContent.trim() === 'PRODUCT_AMOUNT_UNITS.PCS',
        );
        if (pieces === undefined) {
            throw new Error('Expected the piece unit option');
        }
        pieces.click();
        const calories = requiredProductInput(editor.el, '.nutrition-editor__input--calories input');
        const caloriesPerPiece = 35;
        enterProductValue(calories, String(caloriesPerPiece));
        editor.fixture.detectChanges();
        expect(unit.textContent.trim()).toBe('PRODUCT_AMOUNT_UNITS.PCS');

        pending.next(createUsdaNutritionDetail(USDA_PROTEIN_PER_100G));
        pending.complete();
        editor.fixture.detectChanges();

        expect(unit.textContent.trim()).toBe('PRODUCT_AMOUNT_UNITS.PCS');
        expect(calories.value).toBe(String(caloriesPerPiece));
        expect(requiredProductInput(editor.el, '.nutrition-editor__input--proteins input').value).toBe('');
        requiredFormButton(editor.el, 'button[type="submit"]').click();
        await editor.fixture.whenStable();
        expect(editor.productManageFacade.submitProductAsync).toHaveBeenCalledExactlyOnceWith(
            null,
            expect.objectContaining({ baseAmount: 1, baseUnit: MeasurementUnit.PCS, caloriesPerBase: caloriesPerPiece }),
            false,
            expect.any(Function),
        );
    });
});

describe('Product editor USDA lookup from a piece product', () => {
    it.each([false, true])('retains a compatible nutrition basis when the existing piece draft was edited=%s', async edited => {
        const editor = await setupComponentAsync();
        const existing = { ...PRODUCT, baseUnit: MeasurementUnit.PCS, baseAmount: 1, defaultPortionAmount: 1 };
        editor.fixture.componentRef.setInput('product', existing);
        const pending = new Subject<UsdaFoodDetail>();
        editor.externalFoodFacade.getUsdaFoodDetail.mockReturnValueOnce(pending);
        await chooseProductSuggestionAsync(editor, USDA_SUGGESTION);
        const unit = requiredFormButton(editor.el, '.product-manage__serving-row fd-ui-select button');
        const calories = requiredProductInput(editor.el, '.nutrition-editor__input--calories input');
        const pieceEnergy = 35;
        expect(unit.textContent.trim()).toBe('PRODUCT_AMOUNT_UNITS.PCS');
        expect(calories.value).toBe('');
        if (edited) {
            enterProductValue(calories, String(pieceEnergy));
            editor.fixture.detectChanges();
        }

        pending.next(createUsdaNutritionDetail(USDA_PROTEIN_PER_100G));
        pending.complete();
        editor.fixture.detectChanges();

        expect(unit.textContent.trim()).toBe(edited ? 'PRODUCT_AMOUNT_UNITS.PCS' : 'PRODUCT_AMOUNT_UNITS.G');
        expect(calories.value).toBe(String(edited ? pieceEnergy : BASE_CALORIES));
        expect(requiredProductInput(editor.el, '.nutrition-editor__input--proteins input').value).toBe(
            edited ? '' : String(EXPECTED_ROUNDED_USDA_PROTEIN),
        );
    });
});

describe('Product editor refresh while recognition is pending', () => {
    it.each([
        ['opening another product', SECOND_PRODUCT],
        ['refreshing the current product', SAME_ID_UPDATED_PRODUCT],
    ] as const)('keeps the fresh product fields after %s with the same USDA source', async (_context, refreshed) => {
        const editor = await setupComponentAsync();
        const oldProduct = { ...PRODUCT, usdaFdcId: USDA_FDC_ID };
        const currentProduct = { ...refreshed, usdaFdcId: USDA_FDC_ID };
        editor.fixture.componentRef.setInput('product', oldProduct);
        const pending = new Subject<UsdaFoodDetail>();
        editor.externalFoodFacade.getUsdaFoodDetail.mockReturnValueOnce(pending);
        await chooseProductSuggestionAsync(editor, USDA_SUGGESTION);
        expect(pending.observed).toBe(true);
        expect(requiredProductInput(editor.el, 'fd-ui-autocomplete input').value).toBe(USDA_SUGGESTION.name);

        editor.fixture.componentRef.setInput('product', currentProduct);
        editor.fixture.detectChanges();
        const proteins = requiredProductInput(editor.el, '.nutrition-editor__input--proteins input');
        expect(proteins.value).toBe(String(currentProduct.proteinsPerBase));
        pending.next(createUsdaFoodDetail(USDA_FDC_ID, 'Details from the former editor'));
        pending.complete();
        editor.fixture.detectChanges();

        expect(requiredProductInput(editor.el, 'fd-ui-autocomplete input').value).toBe(currentProduct.name);
        expect(proteins.value).toBe(String(currentProduct.proteinsPerBase));
        expect(editor.productManageFacade.submitProductAsync).not.toHaveBeenCalled();
    });

    it('keeps the new product fields when the old AI dialog completes', async () => {
        const editor = await setupComponentAsync();
        const pending = new Subject<ProductAiRecognitionResult>();
        editor.dialogService.open.mockReturnValueOnce({ afterClosed: (): Subject<ProductAiRecognitionResult> => pending });
        editor.fixture.componentRef.setInput('product', PRODUCT);
        editor.fixture.detectChanges();
        requiredFormButton(editor.el, '.product-manage__ai button').click();
        expect(pending.observed).toBe(true);

        editor.fixture.componentRef.setInput('product', SECOND_PRODUCT);
        editor.fixture.detectChanges();
        pending.next({
            name: 'Recognized in the former editor',
            image: null,
            baseUnit: MeasurementUnit.G,
            baseAmount: PORTION_AMOUNT,
            caloriesPerBase: PORTION_CALORIES,
            proteinsPerBase: 1,
            fatsPerBase: 0,
            carbsPerBase: 0,
            fiberPerBase: 0,
            alcoholPerBase: 0,
        });
        pending.complete();
        editor.fixture.detectChanges();

        expect(requiredProductInput(editor.el, 'fd-ui-autocomplete input').value).toBe(SECOND_PRODUCT.name);
        expect(requiredProductInput(editor.el, '.nutrition-editor__input--calories input').value).toBe(
            String(SECOND_PRODUCT.caloriesPerBase),
        );
        expect(editor.productManageFacade.submitProductAsync).not.toHaveBeenCalled();
    });

    it('releases the open AI dialog result subscription when its editor leaves', async () => {
        const editor = await setupComponentAsync();
        const result = new Subject<ProductAiRecognitionResult>();
        const close = vi.fn();
        editor.dialogService.open.mockReturnValueOnce({ afterClosed: (): Subject<ProductAiRecognitionResult> => result, close });
        editor.fixture.detectChanges();
        requiredFormButton(editor.el, '.product-manage__ai button').click();
        expect(result.observed).toBe(true);

        editor.fixture.destroy();

        expect(close).toHaveBeenCalledOnce();
        expect(result.observed).toBe(false);
        expect(editor.productManageFacade.submitProductAsync).not.toHaveBeenCalled();
    });
});

describe('Product editor native source replacement', () => {
    it('saves the chosen Open Food Facts fields and removes the previous USDA link', async () => {
        const editor = await setupComponentAsync();
        const existing = { ...PRODUCT, usdaFdcId: USDA_FDC_ID };
        editor.fixture.componentRef.setInput('product', existing);
        const selected: ProductSearchSuggestion = { ...OFF_PRODUCT, source: 'openFoodFacts' };
        const saved = { ...existing, name: selected.name, brand: selected.brand };
        editor.productManageFacade.submitProductAsync.mockImplementationOnce(async (_product, _request, _skip, afterSave) => {
            await afterSave?.(saved);
            return { product: saved, error: null };
        });
        await chooseProductSuggestionAsync(editor, selected);
        expect(requiredProductInput(editor.el, 'fd-ui-autocomplete input').value).toBe(selected.name);
        expect(requiredProductInput(editor.el, 'input[aria-label="PRODUCT_MANAGE.BARCODE"]').value).toBe(OFF_PRODUCT.barcode);
        expect(requiredProductInput(editor.el, 'input[aria-label="PRODUCT_MANAGE.BRAND"]').value).toBe(OFF_PRODUCT.brand);

        requiredFormButton(editor.el, 'button[type="submit"]').click();
        await editor.fixture.whenStable();

        expect(editor.productManageFacade.submitProductAsync).toHaveBeenCalledExactlyOnceWith(
            existing,
            expect.objectContaining({ name: OFF_PRODUCT.name, barcode: OFF_PRODUCT.barcode, brand: OFF_PRODUCT.brand }),
            false,
            expect.any(Function),
        );
        expect(editor.externalFoodFacade.unlinkUsdaProduct).toHaveBeenCalledExactlyOnceWith(existing.id);
        expect(editor.externalFoodFacade.linkUsdaProduct).not.toHaveBeenCalled();
        expect(editor.externalFoodFacade.searchByBarcode).not.toHaveBeenCalled();
    });
});

describe('Product editor native dialog cancellation', () => {
    it('routes Cancel through the enclosing dialog with no page-level navigation or save', async () => {
        const editor = await setupComponentAsync();
        editor.fixture.componentRef.setInput('mode', 'dialog');
        const cancelled = vi.fn();
        editor.component.cancelled.subscribe(cancelled);
        editor.fixture.detectChanges();
        enterProductValue(requiredProductInput(editor.el, 'fd-ui-autocomplete input'), 'Unsaved label');
        editor.fixture.detectChanges();

        requiredFormButton(editor.el, '.product-manage__cancel-button button').click();
        await editor.fixture.whenStable();

        expect(cancelled).toHaveBeenCalledOnce();
        expect(editor.productManageFacade.confirmDiscardChangesAsync).not.toHaveBeenCalled();
        expect(editor.navigationService.navigateToProductListAsync).not.toHaveBeenCalled();
        expect(editor.productManageFacade.submitProductAsync).not.toHaveBeenCalled();
    });
});

describe('Product editor native recovery', () => {
    it('restores a barcode once and keeps a typed draft when a different prefill arrives', async () => {
        const { el, fixture, externalFoodFacade, productManageFacade } = await setupComponentAsync();
        fixture.detectChanges();
        const barcode = requiredProductInput(el, 'input[aria-label="PRODUCT_MANAGE.BARCODE"]');
        const name = requiredProductInput(el, 'fd-ui-autocomplete input');
        fixture.componentRef.setInput('prefill', { barcode: ' 4600000000001 ' });
        fixture.detectChanges();
        expect(barcode.value).toBe('4600000000001');
        expect(externalFoodFacade.searchByBarcode).toHaveBeenCalledExactlyOnceWith('4600000000001');

        fixture.componentRef.setInput('prefill', { barcode: '4600000000001' });
        fixture.detectChanges();
        expect(externalFoodFacade.searchByBarcode).toHaveBeenCalledTimes(1);

        enterProductValue(name, 'My own product');
        fixture.detectChanges();
        fixture.componentRef.setInput('prefill', { offProduct: OFF_PRODUCT });
        fixture.detectChanges();

        expect(name.value).toBe('My own product');
        expect(barcode.value).toBe('4600000000001');
        expect(externalFoodFacade.searchByBarcode).toHaveBeenCalledTimes(1);
        expect(productManageFacade.submitProductAsync).not.toHaveBeenCalled();
    });

    it('highlights a saved brand that exceeds the current limit and admits a corrected save', async () => {
        const { el, fixture, productManageFacade } = await setupComponentAsync();
        fixture.componentRef.setInput('product', { ...PRODUCT, brand: 'B'.repeat(PRODUCT_BRAND_MAX_LENGTH + 1) });
        fixture.detectChanges();
        const brand = requiredProductInput(el, 'input[aria-label="PRODUCT_MANAGE.BRAND"]');
        const save = requiredFormButton(el, 'button[type="submit"]');
        expect(brand.getAttribute('aria-invalid')).not.toBe('true');
        save.click();
        await fixture.whenStable();
        fixture.detectChanges();
        expect(brand.getAttribute('aria-invalid')).toBe('true');
        expect(productManageFacade.submitProductAsync).not.toHaveBeenCalled();

        enterProductValue(brand, 'Corrected brand');
        fixture.detectChanges();
        expect(brand.getAttribute('aria-invalid')).not.toBe('true');
        save.click();
        await fixture.whenStable();

        expect(productManageFacade.submitProductAsync).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({ id: PRODUCT.id }),
            expect.objectContaining({ brand: 'Corrected brand' }),
            expect.any(Boolean),
            expect.any(Function),
        );
    });
});

describe('Product editor nutrition input', () => {
    it('keeps an unfinished empty nutrition draft editable when the display basis changes', async () => {
        const { el, fixture, productManageFacade } = await setupComponentAsync();
        fixture.detectChanges();
        const portion = requiredFormButton(el, 'fd-ui-segmented-toggle button[role="radio"]:last-child');
        const base = requiredFormButton(el, 'fd-ui-segmented-toggle button[role="radio"]:first-child');
        const calories = requiredProductInput(el, '.nutrition-editor__input--calories input');

        portion.click();
        fixture.detectChanges();
        expect(portion.getAttribute('aria-checked')).toBe('true');
        expect(calories.value).toBe('');

        base.click();
        fixture.detectChanges();
        expect(base.getAttribute('aria-checked')).toBe('true');
        expect(calories.value).toBe('');
        expect(productManageFacade.submitProductAsync).not.toHaveBeenCalled();
    });

    it('shows invalid serving and macro entries before saving corrected decimal values', async () => {
        const { el, fixture, productManageFacade } = await setupComponentAsync();
        fixture.componentRef.setInput('product', PRODUCT);
        fixture.detectChanges();
        const amount = requiredProductInput(el, 'input[aria-label="PRODUCT_MANAGE.DEFAULT_PORTION_AMOUNT"]');
        const proteins = requiredProductInput(el, '.nutrition-editor__input--proteins input');
        const portion = requiredFormButton(el, 'fd-ui-segmented-toggle button[role="radio"]:last-child');
        const save = requiredFormButton(el, 'button[type="submit"]');
        portion.click();
        fixture.detectChanges();
        enterProductValue(amount, '0');
        enterProductValue(proteins, '9..5');
        fixture.detectChanges();
        save.click();
        await fixture.whenStable();
        fixture.detectChanges();

        expect(amount.getAttribute('aria-invalid')).toBe('true');
        expect(proteins.getAttribute('aria-invalid')).toBe('true');
        expect(productManageFacade.submitProductAsync).not.toHaveBeenCalled();

        enterProductValue(amount, '100,5');
        fixture.detectChanges();
        enterProductValue(proteins, '9,5');
        fixture.detectChanges();
        expect(amount.getAttribute('aria-invalid')).not.toBe('true');
        expect(proteins.getAttribute('aria-invalid')).not.toBe('true');
        save.click();
        await fixture.whenStable();

        expect(productManageFacade.submitProductAsync).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({ id: PRODUCT.id }),
            expect.objectContaining({ defaultPortionAmount: 100.5, proteinsPerBase: 9.5 }),
            expect.any(Boolean),
            expect.any(Function),
        );
    });
});

it('clears the calorie mismatch when an optional protein value is removed and saves normalized nutrition', async () => {
    const { el, fixture, productManageFacade } = await setupComponentAsync();
    fixture.componentRef.setInput('product', { ...PRODUCT, fatsPerBase: 0, carbsPerBase: 0 });
    fixture.detectChanges();
    const proteins = requiredProductInput(el, '.nutrition-editor__input--proteins input');
    const warning = (): Element | null => el.querySelector('.nutrition-editor-messages__warning');
    expect(proteins.value).toBe(String(PRODUCT.proteinsPerBase));
    expect(warning()?.textContent).toContain('NUTRITION_EDITOR.CALORIES_MISMATCH_WARNING');

    enterProductValue(proteins, '');
    fixture.detectChanges();
    expect(proteins.value).toBe('');
    expect(warning()).toBeNull();

    requiredFormButton(el, 'button[type="submit"]').click();
    await fixture.whenStable();
    expect(productManageFacade.submitProductAsync).toHaveBeenCalledExactlyOnceWith(
        expect.objectContaining({ id: PRODUCT.id }),
        expect.objectContaining({
            caloriesPerBase: PRODUCT.caloriesPerBase,
            proteinsPerBase: 0,
            fatsPerBase: 0,
            carbsPerBase: 0,
        }),
        expect.any(Boolean),
        expect.any(Function),
    );
});

describe('Product editor zero serving recovery', () => {
    it('preserves per-base nutrition when correcting a rejected zero serving', async () => {
        const editor = await setupComponentAsync();
        const { el, fixture, productManageFacade } = editor;
        fixture.componentRef.setInput('product', PRODUCT);
        fixture.detectChanges();
        selectProductPortionMode(editor);

        const amount = requiredProductInput(el, 'input[aria-label="PRODUCT_MANAGE.DEFAULT_PORTION_AMOUNT"]');
        const proteins = requiredProductInput(el, '.nutrition-editor__input--proteins input');
        const save = requiredFormButton(el, 'button[type="submit"]');
        expect(Number(proteins.value)).toBe(PRODUCT.proteinsPerBase);

        enterProductValue(amount, '0');
        fixture.detectChanges();
        save.click();
        await fixture.whenStable();
        fixture.detectChanges();

        expect(amount.getAttribute('aria-invalid')).toBe('true');
        expect(proteins.getAttribute('aria-invalid')).not.toBe('true');
        expect(Number(proteins.value)).toBe(PRODUCT.proteinsPerBase);
        expect(productManageFacade.submitProductAsync).not.toHaveBeenCalled();

        enterProductValue(amount, String(PORTION_AMOUNT));
        fixture.detectChanges();
        const expectedPortionProteins = 5;
        expect(amount.getAttribute('aria-invalid')).not.toBe('true');
        expect(Number(proteins.value)).toBe(expectedPortionProteins);
        save.click();
        await fixture.whenStable();

        expect(productManageFacade.submitProductAsync).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({ id: PRODUCT.id }),
            expect.objectContaining({
                defaultPortionAmount: PORTION_AMOUNT,
                caloriesPerBase: PRODUCT.caloriesPerBase,
                proteinsPerBase: PRODUCT.proteinsPerBase,
                fatsPerBase: PRODUCT.fatsPerBase,
                carbsPerBase: PRODUCT.carbsPerBase,
            }),
            expect.any(Boolean),
            expect.any(Function),
        );
    });
});

describe('Product editor portion nutrition warning', () => {
    it('shows the serving-limit warning and saves the corrected protein on its per-base basis', async () => {
        const editor = await setupComponentAsync();
        const { el, fixture, productManageFacade } = editor;
        const portionGrams = 250;
        const perBaseCalories = 400;
        const savedProtein = 100;
        const overflowProtein = 251;
        const correctedPortionProtein = 225;
        const correctedBaseProtein = 90;
        fixture.componentRef.setInput('product', {
            ...PRODUCT,
            name: 'Protein isolate',
            defaultPortionAmount: portionGrams,
            caloriesPerBase: perBaseCalories,
            proteinsPerBase: savedProtein,
            fatsPerBase: 0,
            carbsPerBase: 0,
            fiberPerBase: 0,
        });
        fixture.detectChanges();
        selectProductPortionMode(editor);

        const proteins = requiredProductInput(el, '.nutrition-editor__input--proteins input');
        const save = requiredFormButton(el, 'button[type="submit"]');
        const warning = (): Element | null => el.querySelector('.nutrition-editor-messages__warning');
        expect(proteins.value).toBe(String(portionGrams));
        expect(warning()).toBeNull();

        enterProductValue(proteins, String(overflowProtein));
        fixture.detectChanges();
        expect(proteins.value).toBe(String(overflowProtein));
        expect(warning()?.textContent.trim()).toBe('PRODUCT_MANAGE.NUTRITION_WARNINGS.PORTION_LIMIT');

        save.click();
        await fixture.whenStable();
        expect(productManageFacade.submitProductAsync).not.toHaveBeenCalled();

        enterProductValue(proteins, String(correctedPortionProtein));
        fixture.detectChanges();
        expect(warning()).toBeNull();
        save.click();
        await fixture.whenStable();
        expect(productManageFacade.submitProductAsync).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({ id: PRODUCT.id }),
            expect.objectContaining({
                baseUnit: MeasurementUnit.G,
                baseAmount: PRODUCT.baseAmount,
                defaultPortionAmount: portionGrams,
                caloriesPerBase: perBaseCalories,
                proteinsPerBase: correctedBaseProtein,
            }),
            expect.any(Boolean),
            expect.any(Function),
        );
    });
});

describe('Product editor nutrition for a portion measured in pieces', () => {
    it('keeps edited two-piece nutrition when that mode is selected again and saves it for one piece', async () => {
        const editor = await setupComponentAsync();
        const { el, fixture, productManageFacade } = editor;
        const portionPieces = 2;
        const originalPieceCalories = 80;
        const originalPieceProtein = 5;
        const editedPortionCalories = 180;
        const editedPortionProtein = 14;
        const savedPieceCalories = 90;
        const savedPieceProtein = 7;
        fixture.componentRef.setInput('product', {
            ...PRODUCT,
            name: 'Oat bites',
            baseUnit: MeasurementUnit.PCS,
            baseAmount: 1,
            defaultPortionAmount: portionPieces,
            caloriesPerBase: originalPieceCalories,
            proteinsPerBase: originalPieceProtein,
            fatsPerBase: 0,
            carbsPerBase: 0,
            fiberPerBase: 0,
        });
        fixture.detectChanges();
        selectProductPortionMode(editor);

        const calories = requiredProductInput(el, '.nutrition-editor__input--calories input');
        const proteins = requiredProductInput(el, '.nutrition-editor__input--proteins input');
        expect(calories.value).toBe(String(originalPieceCalories * portionPieces));
        expect(proteins.value).toBe(String(originalPieceProtein * portionPieces));

        enterProductValue(calories, String(editedPortionCalories));
        enterProductValue(proteins, String(editedPortionProtein));
        fixture.detectChanges();

        selectProductPortionMode(editor);
        expect(calories.value).toBe(String(editedPortionCalories));
        expect(proteins.value).toBe(String(editedPortionProtein));
        requiredFormButton(el, 'button[type="submit"]').click();
        await fixture.whenStable();

        expect(productManageFacade.submitProductAsync).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({ id: PRODUCT.id }),
            expect.objectContaining({
                baseUnit: MeasurementUnit.PCS,
                baseAmount: 1,
                defaultPortionAmount: portionPieces,
                caloriesPerBase: savedPieceCalories,
                proteinsPerBase: savedPieceProtein,
            }),
            expect.any(Boolean),
            expect.any(Function),
        );
    });
});

describe('Product editor rendered nutrition precision', () => {
    const baseProtein = 0.1;
    const portionProtein = 0.001;
    const portionAmount = 1;
    const proteinPrecision = 7;

    it.each([
        { basis: 'base', displayedProtein: baseProtein },
        { basis: 'portion', displayedProtein: portionProtein },
    ] as const)('preserves the saved nutrition when submitting with the $basis basis selected', async ({ basis, displayedProtein }) => {
        const { el, fixture, productManageFacade } = await setupComponentAsync();
        fixture.componentRef.setInput('product', { ...PRODUCT, defaultPortionAmount: portionAmount, proteinsPerBase: baseProtein });
        fixture.detectChanges();
        const base = requiredFormButton(el, 'fd-ui-segmented-toggle button[role="radio"]:first-child');
        const portion = requiredFormButton(el, 'fd-ui-segmented-toggle button[role="radio"]:last-child');
        const proteins = requiredProductInput(el, '.nutrition-editor__input--proteins input');
        const selected = basis === 'base' ? base : portion;

        for (const option of [portion, base, portion, base, selected, selected]) {
            option.click();
            fixture.detectChanges();
        }
        expect(selected.getAttribute('aria-checked')).toBe('true');
        proteins.focus();
        proteins.blur();
        expect(Number(proteins.value)).toBeCloseTo(displayedProtein, proteinPrecision);
        requiredFormButton(el, 'button[type="submit"]').click();
        await fixture.whenStable();

        expect(productManageFacade.submitProductAsync).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({ id: PRODUCT.id }),
            expect.objectContaining({ defaultPortionAmount: portionAmount, proteinsPerBase: baseProtein }),
            expect.any(Boolean),
            expect.any(Function),
        );
    });

    it('keeps the edited nutrition in the same product when an unrelated prefill arrives', async () => {
        const { el, fixture, productManageFacade } = await setupComponentAsync();
        fixture.componentRef.setInput('product', PRODUCT);
        fixture.detectChanges();
        const proteins = requiredProductInput(el, '.nutrition-editor__input--proteins input');
        enterProductValue(proteins, '12,34');
        fixture.detectChanges();

        fixture.componentRef.setInput('prefill', { barcode: '12345678' });
        fixture.detectChanges();
        expect(proteins.value).toBe('12,34');
        requiredFormButton(el, 'button[type="submit"]').click();
        await fixture.whenStable();
        const expectedProtein = 12.3;
        expect(productManageFacade.submitProductAsync).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({ id: PRODUCT.id }),
            expect.objectContaining({ proteinsPerBase: expectedProtein }),
            expect.any(Boolean),
            expect.any(Function),
        );
    });
});

describe('Product editor legacy cover actions', () => {
    it.each([
        { label: 'asset-only', url: null, assetId: entityId<'image-asset'>('14fc3d4e-412b-47b4-a2eb-0b8d02b5f0cb') },
        { label: 'URL-only', url: 'https://images.example.test/unchanged-cover.jpg', assetId: null },
    ])('keeps an unchanged $label cover and omits gallery replacement when saving', async ({ url, assetId }) => {
        const { el, fixture, component, productManageFacade } = await setupComponentAsync();
        fixture.componentRef.setInput('product', { ...PRODUCT, imageUrl: url, imageAssetId: assetId });
        fixture.detectChanges();
        const details = fixture.debugElement.query(By.directive(ProductBasicInfoComponent)).injector.get(ProductBasicInfoComponent);
        const gallery = fixture.debugElement.query(By.directive(ImageGalleryEditorComponent)).injector.get(ImageGalleryEditorComponent);
        const field = details.form()();
        const original = field.value();
        expect(gallery.photos()).toHaveLength(url === null ? 0 : 1);

        gallery.photos.set(gallery.photos().map(photo => imageSelection(photo.url, photo.assetId)));
        fixture.detectChanges();

        expect(field.value()).toBe(original);
        expect({ dirty: field.dirty(), touched: field.touched(), mayLeave: await component.canLeaveAsync() }).toEqual({
            dirty: false,
            touched: false,
            mayLeave: true,
        });
        requiredFormButton(el, 'button[type="submit"]').click();
        await fixture.whenStable();
        const payload = productManageFacade.submitProductAsync.mock.calls[0]?.[1];
        expect(payload).toMatchObject({ imageUrl: url, imageAssetId: assetId });
        expect(payload).not.toHaveProperty('imageAssetIds');
    });

    it('removes the previous URL-only cover and saves an explicit empty gallery', async () => {
        const { el, fixture, productManageFacade } = await setupComponentAsync();
        const legacyCover = 'https://images.example.test/legacy-cover.jpg';
        fixture.componentRef.setInput('product', { ...PRODUCT, imageUrl: legacyCover });
        fixture.detectChanges();
        expect(el.querySelector('fd-image-gallery-editor img')?.getAttribute('src')).toBe(legacyCover);

        requiredFormButton(el, 'fd-image-gallery-editor .recognition-photos__preview button').click();
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(document.querySelector('.fd-ui-menu-item--danger button')).not.toBeNull();
        });
        requiredFormButton(document.body, '.fd-ui-menu-item--danger button').click();
        fixture.detectChanges();
        expect(el.querySelector('fd-image-gallery-editor img')).toBeNull();

        requiredFormButton(el, 'button[type="submit"]').click();
        await fixture.whenStable();
        expect(productManageFacade.submitProductAsync).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({ id: PRODUCT.id }),
            expect.objectContaining({ imageAssetId: null, imageAssetIds: [] }),
            expect.any(Boolean),
            expect.any(Function),
        );
    });

    it('keeps the URL-only cover when recognition supplies nutrition without new photos', async () => {
        const { el, fixture, dialogService, productManageFacade } = await setupComponentAsync();
        const legacyCover = 'https://images.example.test/legacy-cover.jpg';
        fixture.componentRef.setInput('product', { ...PRODUCT, imageUrl: legacyCover });
        fixture.detectChanges();
        const recognized: ProductAiRecognitionResult = {
            name: 'Recognized product',
            image: null,
            baseUnit: MeasurementUnit.G,
            baseAmount: PORTION_AMOUNT,
            caloriesPerBase: PORTION_CALORIES,
            proteinsPerBase: 5,
            fatsPerBase: 0,
            carbsPerBase: 0,
            fiberPerBase: 0,
            alcoholPerBase: 0,
        };
        dialogService.open.mockReturnValueOnce({ afterClosed: () => of(recognized) });
        requiredFormButton(el, '.product-manage__ai button').click();
        fixture.detectChanges();

        expect(requiredProductInput(el, 'fd-ui-autocomplete input').value).toBe(recognized.name);
        expect(el.querySelector('fd-image-gallery-editor img')?.getAttribute('src')).toBe(legacyCover);
        expect(el.querySelector('.product-manage__footer-error')).toBeNull();
        requiredFormButton(el, 'button[type="submit"]').click();
        await fixture.whenStable();
        const payload = productManageFacade.submitProductAsync.mock.calls[0]?.[1];
        expect(payload).toMatchObject({ name: recognized.name, caloriesPerBase: BASE_CALORIES });
        expect(payload).not.toHaveProperty('imageAssetIds');
    });
});

const LINKED_PRODUCT_PHOTO_URL = 'https://images.example.test/original-linked-photo.jpg';
const UPLOADED_PRODUCT_PHOTO_URL = 'https://images.example.test/new-uploaded-photo.jpg';
const UPLOADED_PRODUCT_PHOTO_ID = '14fc3d4e-412b-47b4-a2eb-0b8d02b5f0cb';

describe('Product editor mixed gallery persistence', () => {
    it('keeps both photos in the draft and blocks saving the unsupported combination', async () => {
        const { el, fixture, productManageFacade } = await openMixedProductGalleryAsync();
        const saveButton = requiredFormButton(el, 'button[type="submit"]');
        const disabled = saveButton.disabled;
        saveButton.click();
        await fixture.whenStable();
        fixture.detectChanges();

        expect({
            disabled,
            warning: el.querySelector('.product-manage__footer-error')?.textContent.trim() ?? null,
            writes: productManageFacade.submitProductAsync.mock.calls.map(([, payload]) => ({
                imageUrl: payload.imageUrl,
                imageAssetId: payload.imageAssetId,
                imageAssetIds: payload.imageAssetIds,
            })),
            photos: productGalleryUrls(el),
        }).toEqual({
            disabled: true,
            warning: 'PRODUCT_MANAGE.MIXED_LEGACY_GALLERY',
            writes: [],
            photos: [LINKED_PRODUCT_PHOTO_URL, UPLOADED_PRODUCT_PHOTO_URL],
        });
    });

    it.each([
        {
            retained: 'the linked cover',
            removeUrl: UPLOADED_PRODUCT_PHOTO_URL,
            retainedUrl: LINKED_PRODUCT_PHOTO_URL,
            assetId: null,
            galleryIds: undefined,
        },
        {
            retained: 'the uploaded cover',
            removeUrl: LINKED_PRODUCT_PHOTO_URL,
            retainedUrl: UPLOADED_PRODUCT_PHOTO_URL,
            assetId: UPLOADED_PRODUCT_PHOTO_ID,
            galleryIds: [UPLOADED_PRODUCT_PHOTO_ID],
        },
    ])('saves $retained after removing the other photo', async ({ removeUrl, retainedUrl, assetId, galleryIds }) => {
        const { el, fixture, productManageFacade } = await openMixedProductGalleryAsync();
        const photo = Array.from(
            el.querySelectorAll<HTMLButtonElement>('fd-image-gallery-editor .recognition-photos__preview button'),
        ).find(button => button.querySelector('img')?.getAttribute('src') === removeUrl);
        if (photo === undefined) {
            throw new Error('The photo selected for removal was missing');
        }
        photo.click();
        const removeSelector = '.fd-ui-menu-item--danger button';
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(document.querySelector(removeSelector)).not.toBeNull();
        });
        requiredFormButton(document.body, removeSelector).click();
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(productGalleryUrls(el)).toEqual([retainedUrl]);
        });

        expect(el.querySelector('.product-manage__footer-error')).toBeNull();
        const save = requiredFormButton(el, 'button[type="submit"]');
        expect(save.disabled).toBe(false);
        save.click();
        await fixture.whenStable();

        expect(productManageFacade.submitProductAsync).toHaveBeenCalledOnce();
        const payload = productManageFacade.submitProductAsync.mock.calls[0]?.[1];
        expect(payload).toMatchObject({ imageUrl: retainedUrl, imageAssetId: assetId });
        expect(payload.imageAssetIds).toEqual(galleryIds);
        expect(Object.hasOwn(payload, 'imageAssetIds')).toBe(galleryIds !== undefined);
    });
});

async function openMixedProductGalleryAsync(): Promise<ProductManageFormSetup> {
    const editor = await setupComponentAsync();
    const { el, fixture, uploadImage } = editor;
    const photo = imageSelection(UPLOADED_PRODUCT_PHOTO_URL, UPLOADED_PRODUCT_PHOTO_ID);
    uploadImage.mockReturnValueOnce(of(photo));
    fixture.componentRef.setInput('product', { ...PRODUCT, imageUrl: LINKED_PRODUCT_PHOTO_URL });
    fixture.detectChanges();
    expect(productGalleryUrls(el)).toEqual([LINKED_PRODUCT_PHOTO_URL]);
    beginNativeGalleryUpload(el);
    await vi.waitFor(() => {
        fixture.detectChanges();
        expect(uploadImage).toHaveBeenCalledOnce();
        expect(productGalleryUrls(el)).toEqual([LINKED_PRODUCT_PHOTO_URL, UPLOADED_PRODUCT_PHOTO_URL]);
    });
    return editor;
}

function productGalleryUrls(el: HTMLElement): Array<string | null> {
    return Array.from(el.querySelectorAll('fd-image-gallery-editor .recognition-photos__preview img'), image => image.getAttribute('src'));
}

const GALLERY_OVERLAP_RECOGNITION: FoodVisionResponse = {
    items: [],
    productLabel: {
        name: 'Recognized product',
        brand: null,
        baseAmount: PORTION_AMOUNT,
        baseUnit: 'g',
        calories: PORTION_CALORIES,
        protein: null,
        fat: null,
        carbs: null,
        fiber: null,
        alcohol: null,
        notes: null,
    },
};

it('waits for the inline gallery upload before recognizing and preserves the admitted photo after Apply', async () => {
    vi.stubGlobal('matchMedia', vi.fn().mockReturnValue({ matches: false }));
    onTestFinished(() => {
        vi.unstubAllGlobals();
    });
    const analyzeFoodImage = vi.fn<ProductAiRecognitionFacade['analyzeFoodImage']>().mockReturnValue(of(GALLERY_OVERLAP_RECOGNITION));
    const recognitionCount = vi.fn<ProductAiRecognitionFacade['recognitionCount']>().mockReturnValue(of(0));
    const { el, fixture, dialogService, uploadImage } = await setupComponentAsync(undefined, [
        { provide: ProductAiRecognitionFacade, useValue: { analyzeFoodImage, recognitionCount } },
    ]);
    const gallery = Array.from({ length: PRODUCT_MAX_PHOTOS - 1 }, (_, index) => ({
        imageAssetId: `stored-product-photo-${index}`,
        imageUrl: `/stored-product-photo-${index}.png`,
    }));
    fixture.componentRef.setInput('product', { ...PRODUCT, images: gallery });
    fixture.detectChanges();
    const pending = new Subject<ImageSelection>();
    const inlinePhoto = imageSelection('/inline-photo.png', 'pending-inline-photo');
    uploadImage.mockReturnValueOnce(pending);
    const realDialogs = TestBed.runInInjectionContext(() => new FdUiDialogService());
    dialogService.open.mockImplementationOnce(
        (component: typeof ProductAiRecognitionDialogComponent, config: FdUiDialogConfig<ProductAiDialogData>) =>
            realDialogs.open(component, config),
    );
    const recognize = requiredFormButton(el, '.product-manage__ai button');
    beginNativeGalleryUpload(el);
    expect(recognize.isConnected).toBe(true);
    expect(recognize.disabled).toBe(false);
    recognize.click();
    expect(dialogService.open).not.toHaveBeenCalled();

    const save = requiredFormButton(el, 'button[type="submit"]');
    await vi.waitFor(() => {
        fixture.detectChanges();
        expect(pending.observed).toBe(true);
        expect(save.disabled).toBe(true);
    });
    expect(recognize.disabled).toBe(true);
    expect(recognize.getAttribute('title')).toBe('DISABLED_HINTS.OPERATION_BUSY');
    pending.next(inlinePhoto);
    pending.complete();
    const expectedPhotos = [...gallery.map(photo => photo.imageUrl), inlinePhoto.url];
    await vi.waitFor(() => {
        fixture.detectChanges();
        expect(recognize.disabled).toBe(false);
        expect(productGalleryUrls(el)).toEqual(expectedPhotos);
    });

    recognize.click();
    fixture.detectChanges();
    const dialog = document.querySelector<HTMLElement>('fd-product-ai-recognition-dialog');
    if (dialog === null) {
        throw new Error('Expected the native recognition dialog.');
    }
    expect(productGalleryUrls(dialog)).toEqual(expectedPhotos);
    const analyze = productAiAction(dialog, 'PRODUCT_AI_DIALOG.ANALYZE');
    analyze.click();
    fixture.detectChanges();
    expect(analyzeFoodImage).toHaveBeenCalledOnce();
    const replacement = dialog.querySelector<HTMLInputElement>('#product-ai-replace');
    if (replacement === null) {
        throw new Error('Expected the native replacement confirmation.');
    }
    replacement.click();
    fixture.detectChanges();
    productAiAction(dialog, 'PRODUCT_AI_DIALOG.APPLY').click();
    await vi.waitFor(() => {
        fixture.detectChanges();
        expect(dialog.isConnected).toBe(false);
        expect(productGalleryUrls(el)).toEqual(expectedPhotos);
    });
    expect(el.querySelector<HTMLInputElement>('fd-ui-autocomplete input')?.value).toBe('Recognized product');
    expect(save.disabled).toBe(false);
});

function productAiAction(dialog: HTMLElement, label: string): HTMLButtonElement {
    const action = Array.from(dialog.querySelectorAll<HTMLButtonElement>('button')).find(button => button.textContent.trim() === label);
    if (action === undefined) {
        throw new Error(`Expected recognition action ${label}.`);
    }
    return action;
}

const LEGACY_GALLERY_PHOTO_COUNT = 3;

it('restores focus to the cover preview button after changing the gallery order and removing a photo', async () => {
    vi.stubGlobal('matchMedia', vi.fn().mockReturnValue({ matches: false }));
    onTestFinished(() => {
        vi.unstubAllGlobals();
    });
    const { el, fixture, dialogService } = await setupComponentAsync();
    const first = { imageAssetId: 'front', imageUrl: '/product-front.jpg' };
    const removed = { imageAssetId: 'side', imageUrl: '/product-side.jpg' };
    const cover = { imageAssetId: 'back', imageUrl: '/product-back.jpg' };
    fixture.componentRef.setInput('product', { ...PRODUCT, images: [first, removed, cover] });
    fixture.detectChanges();

    const triggerSelector = 'fd-image-gallery-editor .recognition-photos__preview button';
    const [, removedTrigger, coverTrigger] = Array.from(el.querySelectorAll<HTMLButtonElement>(triggerSelector));
    const clickPhotoActionAsync = async (trigger: HTMLButtonElement, label: string): Promise<void> => {
        trigger.click();
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(productGalleryMenuLabels()).toContain(label);
        });
        productAiAction(document.body, label).click();
    };
    await clickPhotoActionAsync(coverTrigger, 'PRODUCT_AI_DIALOG.CHOOSE_COVER');
    fixture.detectChanges();
    expect(productGalleryUrls(el)).toEqual([cover.imageUrl, first.imageUrl, removed.imageUrl]);
    expect(el.querySelector(triggerSelector)).toBe(coverTrigger);

    await clickPhotoActionAsync(removedTrigger, 'IMAGE_UPLOAD_FIELD.REMOVE');
    fixture.detectChanges();
    expect(productGalleryUrls(el)).toEqual([cover.imageUrl, first.imageUrl]);
    expect(removedTrigger.isConnected).toBe(false);
    expect(productGalleryMenuLabels()).toEqual([]);

    const realDialogs = TestBed.runInInjectionContext(() => new FdUiDialogService());
    dialogService.open.mockImplementationOnce(
        (component: typeof FdUiImagePreviewDialogComponent, config: FdUiDialogConfig<FdUiImagePreviewDialogData>) =>
            realDialogs.open(component, config),
    );
    await clickPhotoActionAsync(coverTrigger, 'PRODUCT_AI_DIALOG.VIEW_PHOTO');
    const previewSelector = 'fd-ui-image-preview-dialog';
    await vi.waitFor(() => {
        fixture.detectChanges();
        expect(document.querySelector(`${previewSelector} .fd-ui-image-preview-dialog__image`)?.getAttribute('src')).toBe(cover.imageUrl);
    });
    const close = requiredFormButton(document.body, `${previewSelector} .fd-ui-dialog__close-button`);
    close.focus();
    expect(document.activeElement).toBe(close);
    close.click();
    await vi.waitFor(() => {
        fixture.detectChanges();
        expect(document.querySelector(previewSelector)).toBeNull();
        expect(document.activeElement).toBe(coverTrigger);
    });
});

it('views the selected recognized photo without changing a saved cover that has no URL', async () => {
    vi.stubGlobal('matchMedia', vi.fn().mockReturnValue({ matches: false }));
    onTestFinished(() => {
        vi.unstubAllGlobals();
    });
    const { el, fixture, dialogService, productManageFacade } = await setupComponentAsync();
    const legacyAssetId = entityId<'image-asset'>('saved-cover-without-url');
    fixture.componentRef.setInput('product', { ...PRODUCT, imageAssetId: legacyAssetId });
    fixture.detectChanges();
    const front = imageSelection('/product-front.jpg', 'front-photo');
    const back = imageSelection('/product-back.jpg', 'back-photo');
    const recognized: ProductAiRecognitionResult = {
        name: PRODUCT.name,
        image: null,
        images: [front, back],
        baseUnit: MeasurementUnit.G,
        baseAmount: PORTION_AMOUNT,
        caloriesPerBase: PORTION_CALORIES,
        proteinsPerBase: 5,
        fatsPerBase: 0,
        carbsPerBase: 0,
        fiberPerBase: 0,
        alcoholPerBase: 0,
    };
    dialogService.open.mockReturnValueOnce({ afterClosed: () => of(recognized) });
    requiredFormButton(el, '.product-manage__ai button').click();
    fixture.detectChanges();
    const realDialogs = TestBed.runInInjectionContext(() => new FdUiDialogService());
    dialogService.open.mockImplementationOnce(
        (component: typeof FdUiImagePreviewDialogComponent, config: FdUiDialogConfig<FdUiImagePreviewDialogData>) =>
            realDialogs.open(component, config),
    );

    const triggers = el.querySelectorAll<HTMLButtonElement>('fd-image-gallery-editor .recognition-photos__preview button');
    expect(triggers).toHaveLength(LEGACY_GALLERY_PHOTO_COUNT);
    expect(triggers[1].querySelector('img')?.getAttribute('src')).toBe(front.url);
    triggers[1].click();
    await vi.waitFor(() => {
        fixture.detectChanges();
        expect(productGalleryMenuLabels()).toContain('PRODUCT_AI_DIALOG.VIEW_PHOTO');
    });
    const view = Array.from(document.querySelectorAll<HTMLButtonElement>('button[role="menuitem"]')).find(
        button => button.textContent.trim() === 'PRODUCT_AI_DIALOG.VIEW_PHOTO',
    );
    view?.click();
    await vi.waitFor(() => {
        fixture.detectChanges();
        expect(document.querySelector('fd-ui-image-preview-dialog .fd-ui-image-preview-dialog__image')).not.toBeNull();
    });
    const visiblePhoto = document.querySelector('.fd-ui-image-preview-dialog__image')?.getAttribute('src');
    const thumbnailUrls = Array.from(document.querySelectorAll('.fd-ui-image-preview-dialog__thumbnail img'), image =>
        image.getAttribute('src'),
    );
    requiredFormButton(document.body, 'fd-ui-image-preview-dialog .fd-ui-dialog__close-button').click();
    await fixture.whenStable();
    const legacyHadImage = triggers[0].querySelector('img:not([hidden])') !== null;
    triggers[0].click();
    await vi.waitFor(() => {
        fixture.detectChanges();
        expect(productGalleryMenuLabels()).toContain('IMAGE_UPLOAD_FIELD.REMOVE');
    });
    const legacyActions = productGalleryMenuLabels();
    document.querySelector('[role="menu"]')?.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
    await fixture.whenStable();
    requiredFormButton(el, 'button[type="submit"]').click();
    await fixture.whenStable();
    expect(productManageFacade.submitProductAsync.mock.calls[0]?.[1]).toMatchObject({
        imageUrl: null,
        imageAssetId: legacyAssetId,
        imageAssetIds: [legacyAssetId, front.assetId, back.assetId],
    });
    expect({ visiblePhoto, thumbnailUrls, legacyHadImage, legacyActions }).toEqual({
        visiblePhoto: front.url,
        thumbnailUrls: [front.url, back.url],
        legacyHadImage: false,
        legacyActions: ['IMAGE_UPLOAD_FIELD.REMOVE'],
    });
});

function productGalleryMenuLabels(): string[] {
    return Array.from(document.querySelectorAll('button[role="menuitem"]'))
        .filter(button => button.closest('[hidden]') === null)
        .map(button => button.textContent.trim());
}

describe('Product editor first recognized photos', () => {
    it('saves the selected cover and gallery when the existing product has no photos', async () => {
        const { el, fixture, dialogService, productManageFacade } = await setupComponentAsync();
        fixture.componentRef.setInput('product', PRODUCT);
        fixture.detectChanges();
        expect(el.querySelector('fd-image-gallery-editor img')).toBeNull();

        const cover = imageSelection('https://images.example.test/recognized-front.jpg', 'front-photo');
        const otherPhoto = imageSelection('https://images.example.test/recognized-back.jpg', 'back-photo');
        const recognized: ProductAiRecognitionResult = {
            name: 'Recognized product with photos',
            image: cover,
            images: [cover, otherPhoto],
            baseUnit: MeasurementUnit.G,
            baseAmount: PORTION_AMOUNT,
            caloriesPerBase: PORTION_CALORIES,
            proteinsPerBase: 5,
            fatsPerBase: 0,
            carbsPerBase: 0,
            fiberPerBase: 0,
            alcoholPerBase: 0,
        };
        dialogService.open.mockReturnValueOnce({ afterClosed: () => of(recognized) });
        requiredFormButton(el, '.product-manage__ai button').click();
        fixture.detectChanges();

        expect(requiredProductInput(el, 'fd-ui-autocomplete input').value).toBe(recognized.name);
        expect(Array.from(el.querySelectorAll('fd-image-gallery-editor img'), image => image.getAttribute('src'))).toEqual([
            cover.url,
            otherPhoto.url,
        ]);
        expect(el.querySelector('.product-manage__footer-error')).toBeNull();
        requiredFormButton(el, 'button[type="submit"]').click();
        await fixture.whenStable();

        expect(productManageFacade.submitProductAsync).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({ id: PRODUCT.id }),
            expect.objectContaining({
                name: recognized.name,
                defaultPortionAmount: PORTION_AMOUNT,
                caloriesPerBase: BASE_CALORIES,
                imageUrl: cover.url,
                imageAssetId: cover.assetId,
                imageAssetIds: [cover.assetId, otherPhoto.assetId],
            }),
            expect.any(Boolean),
            expect.any(Function),
        );
    });
});

function selectProductPortionMode(editor: ProductManageFormSetup): void {
    const buttons = Array.from(editor.el.querySelectorAll<HTMLButtonElement>('.product-manage__nutrition-toggle button[role="radio"]'));
    const portion = buttons.find(button => button.textContent.trim() === 'PRODUCT_MANAGE.NUTRITION_MODE.PORTION');
    if (portion === undefined) {
        throw new Error('Expected the portion nutrition mode');
    }
    portion.click();
    editor.fixture.detectChanges();
    expect(portion.getAttribute('aria-checked')).toBe('true');
}

async function chooseProductSuggestionAsync(editor: ProductManageFormSetup, suggestion: ProductSearchSuggestion): Promise<void> {
    stubScrollIntoView();
    editor.searchSuggestions.mockReturnValueOnce(of([suggestion]));
    editor.fixture.detectChanges();
    const name = requiredProductInput(editor.el, 'fd-ui-autocomplete input');
    name.focus();
    enterProductValue(name, 'Source search');
    await vi.waitFor(() => {
        editor.fixture.detectChanges();
        expect(document.querySelector('button[role="option"] .fd-ui-autocomplete__option-label')?.textContent).toBe(suggestion.name);
    });
    requiredFormButton(document.body, 'button.fd-ui-autocomplete__menu-item[role="option"]').click();
    editor.fixture.detectChanges();
}

function requiredProductInput(host: HTMLElement, selector: string): HTMLInputElement {
    const input = host.querySelector(selector);
    if (!(input instanceof HTMLInputElement)) {
        throw new Error(`Missing product editor input ${selector}`);
    }
    return input;
}

function enterProductValue(input: HTMLInputElement, value: string): void {
    input.value = value;
    input.dispatchEvent(new Event('input', { bubbles: true }));
}

function requiredFormButton(host: HTMLElement, selector: string): HTMLButtonElement {
    const button = host.querySelector<HTMLButtonElement>(selector);
    if (button === null) {
        throw new Error(`Missing product editor button ${selector}`);
    }
    return button;
}

function beginNativeGalleryUpload(host: HTMLElement): void {
    const decoded = document.createElement('img');
    const thumbnailSize = 320;
    Object.defineProperties(decoded, { naturalWidth: { value: thumbnailSize }, naturalHeight: { value: thumbnailSize } });
    vi.stubGlobal('Image', function createDecodedImage(): HTMLImageElement {
        return decoded;
    });
    const createUrl = vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:product-thumbnail');
    const revokeUrl = vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => {});
    onTestFinished(() => {
        createUrl.mockRestore();
        revokeUrl.mockRestore();
        vi.unstubAllGlobals();
    });
    const picker = host.querySelector<HTMLInputElement>('fd-image-gallery-editor input[type="file"]');
    if (picker === null) {
        throw new Error('Product gallery file picker is missing');
    }
    Object.defineProperty(picker, 'files', { value: [new File(['thumbnail bytes'], 'thumbnail.png', { type: 'image/png' })] });
    picker.dispatchEvent(new Event('change', { bubbles: true }));
    decoded.dispatchEvent(new Event('load'));
}

async function setupComponentAsync(
    developmentScreens?: {
        attach: (screen: DevelopmentScreen) => () => void;
    },
    additionalProviders: Provider[] = [],
): Promise<ProductManageFormSetup> {
    const productManageFacade = createProductManageFacadeMock();
    const externalFoodFacade = createProductExternalFoodFacadeMock();
    const uploadImage = vi.fn<ImageUploadFacade['upload']>();
    const navigationService = {
        navigateToProductListAsync: vi.fn<NavigationService['navigateToProductListAsync']>().mockResolvedValue(void 0),
    };
    const dialogService = {
        open: vi.fn().mockReturnValue({ afterClosed: () => of(null) }),
    };
    const tourService = {
        start: vi.fn(),
    };
    const searchSuggestions = vi.fn<ProductService['searchSuggestions']>().mockReturnValue(of([]));

    await TestBed.configureTestingModule({
        imports: [ProductManageFormComponent],
        providers: [
            ...(developmentScreens === undefined ? [] : [{ provide: DEVELOPMENT_SCREENS, useValue: developmentScreens }]),
            ...additionalProviders,
            provideRouter([]),
            provideTranslateTesting(),
            { provide: NAME_SEARCH_DEBOUNCE_MS, useValue: 0 },
            {
                provide: ProductService,
                useValue: {
                    searchSuggestions,
                },
            },
            { provide: ProductExternalFoodFacade, useValue: externalFoodFacade },
            { provide: ImageUploadFacade, useValue: { upload: uploadImage } },
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
            {
                provide: FdTourService,
                useValue: tourService,
            },
        ],
    })
        .overrideComponent(ProductManageFormComponent, { remove: { providers: [ProductManageFacade] } })
        .compileComponents();

    const fixture = TestBed.createComponent(ProductManageFormComponent);
    return {
        fixture,
        el: fixture.nativeElement as HTMLElement,
        component: fixture.componentInstance,
        dialogService,
        productManageFacade,
        uploadImage,
        navigationService,
        tourService,
        externalFoodFacade,
        searchSuggestions,
    };
}

function createProductManageFacadeMock(): ProductManageFacadeMock {
    return {
        committed$: new Subject<void>(),
        ensurePremiumAccess: vi.fn().mockReturnValue(true),
        confirmDiscardChangesAsync: vi.fn().mockResolvedValue(true),
        deleteProductAsync: vi.fn().mockResolvedValue('deleted'),
        submitProductAsync: vi.fn<ProductManageFacade['submitProductAsync']>().mockResolvedValue({ product: null, error: null }),
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
    patchProductForm(component, {
        name: 'Valid product',
        caloriesPerBase: 100,
        proteinsPerBase: 10,
    });
}

function productValues(component: ProductManageFormComponent): ProductFormValues {
    return component['productFormModel']();
}

function patchProductForm(component: ProductManageFormComponent, patch: Partial<ProductFormValues>): void {
    component['patchProductForm'](patch);
}

function createUsdaFoodDetail(fdcId: number, description: string): UsdaFoodDetail {
    return {
        fdcId: usdaFoodId(fdcId),
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

function createUsdaNutritionDetail(proteins: number): UsdaFoodDetail {
    const detail = createUsdaFoodDetail(USDA_FDC_ID, 'Provider description');
    return {
        ...detail,
        nutrients: [
            ...detail.nutrients.map(nutrient => ({ ...nutrient, amountPer100g: proteins })),
            {
                nutrientId: USDA_NUTRIENT_IDS.energy,
                name: 'Energy',
                unit: 'kcal',
                amountPer100g: BASE_CALORIES,
                dailyValue: null,
                percentDailyValue: null,
            },
        ],
    };
}
