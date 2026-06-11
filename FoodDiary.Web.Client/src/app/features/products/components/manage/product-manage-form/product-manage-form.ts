import { type HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, inject, input, output, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { form, min, required } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiFormErrorComponent } from 'fd-ui-kit/form-error/fd-ui-form-error';
import { catchError, firstValueFrom, of } from 'rxjs';

import { BarcodeScannerComponent } from '../../../../../components/shared/barcode-scanner/barcode-scanner';
import type { ConfirmDeleteDialogData } from '../../../../../components/shared/confirm-delete-dialog/confirm-delete-dialog';
import { ManageHeaderComponent } from '../../../../../components/shared/manage-header/manage-header';
import { PageBodyComponent } from '../../../../../components/shared/page-body/page-body';
import { NavigationService } from '../../../../../services/navigation.service';
import { checkMacrosError } from '../../../../../shared/lib/nutrition-form.utils';
import { patchSignalFormModel } from '../../../../../shared/lib/signal-form-model.utils';
import { getRecordProperty } from '../../../../../shared/lib/unknown-value.utils';
import { FdPageContainerDirective } from '../../../../../shared/ui/layout/page-container.directive';
import type { UsdaFoodDetail } from '../../../../usda/models/usda.data';
import { ProductAiRecognitionDialogComponent } from '../../../dialogs/product-ai-recognition-dialog/product-ai-recognition-dialog';
import type { ProductAiRecognitionResult } from '../../../dialogs/product-ai-recognition-dialog/product-ai-recognition-dialog.types';
import { ProductExternalFoodFacade } from '../../../lib/manage/product-external-food.facade';
import { ProductNameSearchFacade } from '../../../lib/manage/product-name-search.facade';
import {
    buildOpenFoodFactsLookupPatch,
    buildResetNutritionPatch,
    buildSourceProductPrefillPatch,
    buildUsdaFoodDetailPrefillPatch,
} from '../../../lib/manage/product-nutrition-prefill.mapper';
import { PRODUCT_MIN_AMOUNT } from '../../../lib/product-manage.constants';
import { ProductManageFacade } from '../../../lib/product-manage.facade';
import type { OpenFoodFactsProduct } from '../../../models/open-food-facts.data';
import type { Product } from '../../../models/product.data';
import { ProductBasicInfoComponent } from '../product-basic-info/product-basic-info';
import {
    buildAiResultPatch,
    buildConvertedNutritionPatch,
    buildProductData,
    buildProductFormPatch,
    createProductForm,
    getDefaultProductBaseAmount,
    getProductControlNumberValue,
} from '../product-manage-lib/product-manage-form.mapper';
import type {
    NutritionMode,
    ProductFormValues,
    ProductManageMode,
    ProductManagePrefill,
} from '../product-manage-lib/product-manage-form.types';
import type { ProductNameSuggestion } from '../product-manage-lib/product-name-search.types';
import { ProductNutritionEditorComponent } from '../product-nutrition-editor/product-nutrition-editor';

@Component({
    selector: 'fd-product-manage-form',
    templateUrl: './product-manage-form.html',
    styleUrls: ['./product-manage-form.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [ProductNameSearchFacade],
    imports: [
        TranslatePipe,
        FdUiHintDirective,
        FdUiButtonComponent,
        FdUiFormErrorComponent,
        FdPageContainerDirective,
        ManageHeaderComponent,
        PageBodyComponent,
        ProductBasicInfoComponent,
        ProductNutritionEditorComponent,
    ],
})
export class ProductManageFormComponent {
    protected readonly translateService = inject(TranslateService);
    protected readonly navigationService = inject(NavigationService);
    protected readonly fdDialogService = inject(FdUiDialogService);
    protected readonly nameSearch = inject(ProductNameSearchFacade);
    private readonly destroyRef = inject(DestroyRef);
    private readonly productManageFacade = inject(ProductManageFacade);
    private readonly externalFoodFacade = inject(ProductExternalFoodFacade);

    public readonly product = input<Product | null>(null);
    public readonly prefill = input<ProductManagePrefill | null>(null);
    public readonly mode = input<ProductManageMode>('page');
    public readonly saved = output<Product>();
    public readonly cancelled = output();
    protected readonly globalError = signal<string | null>(null);
    private populatedProduct: Product | null = null;
    private appliedPrefillKey: string | null = null;
    private usdaDetailRequestId = 0;
    protected readonly isDeleting = signal(false);
    protected readonly isSubmitting = signal(false);

    protected readonly productFormModel = signal<ProductFormValues>(createProductForm());
    protected readonly productForm = form(this.productFormModel, path => {
        required(path.name);
        required(path.baseAmount);
        min(path.baseAmount, PRODUCT_MIN_AMOUNT);
        required(path.defaultPortionAmount);
        min(path.defaultPortionAmount, PRODUCT_MIN_AMOUNT);
        required(path.baseUnit);
        required(path.caloriesPerBase);
        min(path.caloriesPerBase, 0);
        min(path.proteinsPerBase, 0);
        min(path.fatsPerBase, 0);
        min(path.carbsPerBase, 0);
        min(path.fiberPerBase, 0);
        min(path.alcoholPerBase, 0);
        required(path.visibility);
    });
    protected nutritionMode: NutritionMode = 'base';
    private previousBaseUnit = this.productFormModel().baseUnit;

    public constructor() {
        this.bindFormEffects();
    }

    private bindFormEffects(): void {
        effect(() => {
            const unit = this.productFormModel().baseUnit;
            if (unit === this.previousBaseUnit) {
                return;
            }

            this.previousBaseUnit = unit;
            untracked(() => {
                this.patchProductForm({ baseAmount: getDefaultProductBaseAmount(unit) });
            });
        });

        effect(() => {
            const currentProduct = this.product();
            if (currentProduct !== null) {
                if (this.populatedProduct === currentProduct) {
                    return;
                }
                this.populateForm(currentProduct);
                this.populatedProduct = currentProduct;
                this.appliedPrefillKey = null;
                return;
            }

            this.populatedProduct = null;
            this.applyPrefillIfNeeded(this.prefill());
        });
        effect(() => {
            this.productFormModel();
            this.clearGlobalError();
        });
    }

    private applyPrefillIfNeeded(prefill: ProductManagePrefill | null): void {
        const key = this.getPrefillKey(prefill);
        if (key === null || key === this.appliedPrefillKey || this.productForm().dirty()) {
            return;
        }

        this.appliedPrefillKey = key;
        if (prefill === null) {
            return;
        }

        if (prefill.offProduct !== null && prefill.offProduct !== undefined) {
            this.prefillFromOffProduct(prefill.offProduct);
            return;
        }

        this.applyBarcodePrefill(prefill.barcode ?? null);
    }

    private applyBarcodePrefill(barcodeValue: string | null): void {
        const barcode = barcodeValue?.trim();
        if (barcode === undefined || barcode.length === 0) {
            return;
        }

        this.patchProductForm({ barcode });
        this.lookupOpenFoodFacts(barcode);
    }

    private getPrefillKey(prefill: ProductManagePrefill | null): string | null {
        if (prefill === null) {
            return null;
        }

        const offProduct = prefill.offProduct;
        if (offProduct !== null && offProduct !== undefined) {
            return `off:${offProduct.barcode}`;
        }

        const barcode = prefill.barcode?.trim();
        return barcode === undefined || barcode.length === 0 ? null : `barcode:${barcode}`;
    }

    protected readonly canShowDeleteButton = computed(() => {
        const currentProduct = this.product();
        return currentProduct?.isOwnedByCurrentUser === true;
    });
    protected readonly manageHeaderState = computed<ProductManageHeaderState>(() => {
        const isEdit = this.product() !== null;

        return {
            titleKey: isEdit ? 'PRODUCT_MANAGE.EDIT_TITLE' : 'PRODUCT_MANAGE.ADD_TITLE',
            submitIcon: isEdit ? 'save' : 'add',
            submitLabelKey: isEdit ? 'PRODUCT_MANAGE.SAVE_BUTTON' : 'PRODUCT_MANAGE.ADD_BUTTON',
        };
    });

    protected openBarcodeScanner(): void {
        this.fdDialogService
            .open<BarcodeScannerComponent, null, string | null>(BarcodeScannerComponent, { size: 'lg' })
            .afterClosed()
            .subscribe(barcode => {
                if (barcode !== null && barcode !== undefined && barcode.length > 0) {
                    this.setBarcodeFromUserAction(barcode);
                    this.lookupOpenFoodFacts(barcode);
                }
            });
    }

    private setBarcodeFromUserAction(barcode: string): void {
        this.productForm.barcode().value.set(barcode);
        this.productForm.barcode().markAsDirty();
    }

    private lookupOpenFoodFacts(barcode: string): void {
        if (this.product() !== null) {
            return;
        }

        this.externalFoodFacade
            .searchByBarcode(barcode)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(offProduct => {
                if (offProduct === null || this.product() !== null || this.productFormModel().barcode !== barcode) {
                    return;
                }

                this.patchProductForm(buildOpenFoodFactsLookupPatch(this.productFormModel(), offProduct));
            });
    }

    private prefillFromOffProduct(offProduct: OpenFoodFactsProduct | ProductNameSuggestion): void {
        this.patchProductForm(buildSourceProductPrefillPatch(offProduct));
    }

    private prefillFromUsdaFoodDetail(detail: UsdaFoodDetail): void {
        this.patchProductForm(buildUsdaFoodDetailPrefillPatch(detail));
    }

    protected openAiRecognitionDialog(): void {
        if (!this.ensurePremiumAccess()) {
            return;
        }

        this.fdDialogService
            .open<ProductAiRecognitionDialogComponent, { initialDescription?: string | null }, ProductAiRecognitionResult | null>(
                ProductAiRecognitionDialogComponent,
                {
                    size: 'lg',
                    data: {
                        initialDescription: this.productFormModel().description ?? null,
                    },
                },
            )
            .afterClosed()
            .subscribe(result => {
                if (result === null || result === undefined) {
                    return;
                }

                this.applyAiResult(result);
            });
    }

    protected async onCancelAsync(): Promise<void> {
        if (this.mode() === 'dialog') {
            this.cancelled.emit();
            return;
        }

        if (this.hasUnsavedChanges()) {
            const shouldLeave = await this.confirmDiscardChangesAsync();
            if (!shouldLeave) {
                return;
            }
        }

        await this.navigationService.navigateToProductListAsync();
    }

    protected async onDeleteProductAsync(): Promise<void> {
        const currentProduct = this.product();

        if (currentProduct === null || !currentProduct.isOwnedByCurrentUser || this.isDeleting() || this.isSubmitting()) {
            return;
        }

        const data: ConfirmDeleteDialogData = {
            title: this.translateService.instant('CONFIRM_DELETE.TITLE', {
                type: this.translateService.instant('PRODUCT_DETAIL.ENTITY_NAME'),
            }),
            message: this.translateService.instant('CONFIRM_DELETE.MESSAGE', { name: currentProduct.name }),
            name: currentProduct.name,
            entityType: this.translateService.instant('PRODUCT_DETAIL.ENTITY_NAME'),
            confirmLabel: this.translateService.instant('CONFIRM_DELETE.CONFIRM'),
            cancelLabel: this.translateService.instant('CONFIRM_DELETE.CANCEL'),
        };

        this.isDeleting.set(true);
        this.clearGlobalError();

        const result = await this.productManageFacade.deleteProductAsync(currentProduct, data);
        if (result !== 'deleted') {
            this.isDeleting.set(false);
            if (result === 'error') {
                this.setGlobalError('PRODUCT_MANAGE.DELETE_ERROR');
            }
        }
    }

    protected async onSubmitAsync(): Promise<Product | null> {
        if (this.isSubmitting() || this.isDeleting()) {
            return null;
        }

        this.productForm().markAsTouched();

        if (this.hasMacrosError() || this.productForm().invalid()) {
            return null;
        }

        this.isSubmitting.set(true);
        const currentValues = this.productFormModel();
        const productData = buildProductData(currentValues, this.nutritionMode);
        const product = this.product() ?? null;
        const nextUsdaFdcId = currentValues.usdaFdcId;
        const previousUsdaFdcId = product?.usdaFdcId ?? null;

        try {
            const result = await this.productManageFacade.submitProductAsync(
                product,
                productData,
                this.shouldSkipSubmitConfirmDialog(),
                async savedProduct => this.syncUsdaLinkAsync(savedProduct, nextUsdaFdcId, previousUsdaFdcId),
            );
            this.handleSubmitResult(result);
            return result.product;
        } finally {
            this.isSubmitting.set(false);
        }
    }

    protected onNutritionModeChange(nextMode: string): void {
        const resolvedMode: NutritionMode = nextMode === 'portion' ? 'portion' : 'base';
        if (resolvedMode === this.nutritionMode) {
            return;
        }

        const values = this.productFormModel();
        const baseAmount = getDefaultProductBaseAmount(values.baseUnit);
        const portionAmount = getProductControlNumberValue(values.defaultPortionAmount);

        if (portionAmount > 0) {
            const factor = resolvedMode === 'portion' ? portionAmount / baseAmount : baseAmount / portionAmount;
            this.convertNutritionControls(factor);
        }

        this.nutritionMode = resolvedMode;
    }

    protected onNameQueryChange(query: string): void {
        this.nameSearch.search(query);
    }

    protected onNameSuggestionSelected(suggestion: ProductNameSuggestion): void {
        if (suggestion.source === 'usda') {
            const fdcId = suggestion.usdaFdcId;
            if (fdcId === null || fdcId === undefined) {
                return;
            }

            this.patchProductForm({
                name: suggestion.name,
                barcode: null,
                brand: null,
                usdaFdcId: fdcId,
            });
            this.patchProductForm(buildResetNutritionPatch());
            this.nameSearch.setSelectedSuggestion(suggestion);
            const requestId = ++this.usdaDetailRequestId;
            this.externalFoodFacade
                .getUsdaFoodDetail(fdcId)
                .pipe(
                    catchError(() => of<UsdaFoodDetail | null>(null)),
                    takeUntilDestroyed(this.destroyRef),
                )
                .subscribe(detail => {
                    if (detail !== null && requestId === this.usdaDetailRequestId && this.productFormModel().usdaFdcId === fdcId) {
                        this.prefillFromUsdaFoodDetail(detail);
                    }
                });
            return;
        }

        this.prefillFromOffProduct(suggestion);
        this.productForm.usdaFdcId().value.set(null);
        this.nameSearch.setSelectedSuggestion(suggestion);
    }

    private hasMacrosError(): boolean {
        const controls = [
            this.getNutritionControlState('proteinsPerBase'),
            this.getNutritionControlState('fatsPerBase'),
            this.getNutritionControlState('carbsPerBase'),
            this.getNutritionControlState('alcoholPerBase'),
        ];

        return checkMacrosError(controls);
    }

    private convertNutritionControls(factor: number): void {
        const patch = buildConvertedNutritionPatch(this.productFormModel(), factor);

        if (Object.keys(patch).length > 0) {
            this.patchProductForm(patch);
        }
    }

    private populateForm(product: Product): void {
        this.patchProductForm(buildProductFormPatch(product));
        this.nutritionMode = 'base';
    }

    private applyAiResult(result: ProductAiRecognitionResult): void {
        this.patchProductForm(buildAiResultPatch(this.productFormModel(), result));
        this.nutritionMode = 'portion';
    }

    private handleSubmitError(error: unknown): void {
        const status = getRecordProperty(error, 'status');
        if (status === HttpStatusCode.Unauthorized) {
            this.setGlobalError('FORM_ERRORS.UNAUTHORIZED');
        } else if (status === HttpStatusCode.BadRequest) {
            this.setGlobalError('FORM_ERRORS.INVALID_DATA');
        } else {
            this.setGlobalError('FORM_ERRORS.UNKNOWN');
        }
    }

    private handleSubmitResult(result: ProductSubmitResult): void {
        if (result.error !== null) {
            if (result.product === null) {
                this.handleSubmitError(result.error);
            } else {
                this.setGlobalError('PRODUCT_MANAGE.USDA_SYNC_ERROR');
            }
        }
        if (result.product !== null) {
            this.saved.emit(result.product);
        }
    }

    private setGlobalError(errorKey: string): void {
        this.globalError.set(this.translateService.instant(errorKey));
    }

    private clearGlobalError(): void {
        this.globalError.set(null);
    }

    private async syncUsdaLinkAsync(savedProduct: Product, nextFdcId: number | null, previousFdcId: number | null): Promise<void> {
        if (nextFdcId === previousFdcId) {
            return;
        }

        if (nextFdcId !== null) {
            await firstValueFrom(this.externalFoodFacade.linkUsdaProduct(savedProduct.id, nextFdcId));
            return;
        }

        if (previousFdcId !== null) {
            await firstValueFrom(this.externalFoodFacade.unlinkUsdaProduct(savedProduct.id));
        }
    }

    private hasUnsavedChanges(): boolean {
        return this.productForm().dirty();
    }

    private async confirmDiscardChangesAsync(): Promise<boolean> {
        const data: ConfirmDeleteDialogData = {
            title: this.translateService.instant('PRODUCT_MANAGE.LEAVE_CONFIRM_TITLE'),
            message: this.translateService.instant('PRODUCT_MANAGE.LEAVE_CONFIRM_MESSAGE'),
            confirmLabel: this.translateService.instant('PRODUCT_MANAGE.LEAVE_CONFIRM_BUTTON'),
            cancelLabel: this.translateService.instant('PRODUCT_MANAGE.LEAVE_STAY_BUTTON'),
        };
        return this.productManageFacade.confirmDiscardChangesAsync(data);
    }

    private ensurePremiumAccess(): boolean {
        return this.productManageFacade.ensurePremiumAccess();
    }

    private shouldSkipSubmitConfirmDialog(): boolean {
        return this.mode() === 'dialog';
    }

    private patchProductForm(patch: Partial<ProductFormValues>): void {
        patchSignalFormModel(this.productFormModel, patch);
    }

    private getNutritionControlState(
        field: keyof Pick<ProductFormValues, 'proteinsPerBase' | 'fatsPerBase' | 'carbsPerBase' | 'alcoholPerBase'>,
    ): {
        value: number | null;
        touched: boolean;
        dirty: boolean;
    } {
        const state = this.productForm[field]();
        return {
            value: this.productFormModel()[field],
            touched: state.touched(),
            dirty: state.dirty(),
        };
    }
}

type ProductManageHeaderState = {
    titleKey: string;
    submitIcon: ProductManageSubmitIcon;
    submitLabelKey: string;
};

type ProductManageSubmitIcon = 'save' | 'add';

type ProductSubmitResult = {
    product: Product | null;
    error: HttpErrorResponse | null;
};
