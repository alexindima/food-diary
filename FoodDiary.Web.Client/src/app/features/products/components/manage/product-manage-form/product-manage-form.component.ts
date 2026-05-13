import { HttpStatusCode } from '@angular/common/http';
import {
    ChangeDetectionStrategy,
    Component,
    computed,
    DestroyRef,
    effect,
    type FactoryProvider,
    inject,
    input,
    output,
    signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { type FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import {
    FD_VALIDATION_ERRORS,
    FdUiFormErrorComponent,
    type FdValidationErrors,
    getNumberProperty,
} from 'fd-ui-kit/form-error/fd-ui-form-error.component';
import { catchError, firstValueFrom, of } from 'rxjs';

import { BarcodeScannerComponent } from '../../../../../components/shared/barcode-scanner/barcode-scanner.component';
import type { ConfirmDeleteDialogData } from '../../../../../components/shared/confirm-delete-dialog/confirm-delete-dialog.component';
import { ManageHeaderComponent } from '../../../../../components/shared/manage-header/manage-header.component';
import { FdPageContainerDirective } from '../../../../../directives/layout/page-container.directive';
import { NavigationService } from '../../../../../services/navigation.service';
import { checkMacrosError } from '../../../../../shared/lib/nutrition-form.utils';
import { getRecordProperty } from '../../../../../shared/lib/unknown-value.utils';
import { UsdaService } from '../../../../usda/api/usda.service';
import type { UsdaFoodDetail } from '../../../../usda/models/usda.data';
import { type OpenFoodFactsProduct, OpenFoodFactsService } from '../../../api/open-food-facts.service';
import { ProductAiRecognitionDialogComponent } from '../../../dialogs/product-ai-recognition-dialog/product-ai-recognition-dialog.component';
import type { ProductAiRecognitionResult } from '../../../dialogs/product-ai-recognition-dialog/product-ai-recognition-dialog.types';
import { ProductManageFacade } from '../../../lib/product-manage.facade';
import type { Product } from '../../../models/product.data';
import { ProductBasicInfoComponent } from '../product-basic-info/product-basic-info.component';
import {
    buildAiResultPatch,
    buildConvertedNutritionPatch,
    buildProductData,
    buildProductFormPatch,
    createProductForm,
    getDefaultProductBaseAmount,
    getProductControlNumberValue,
} from '../product-manage-lib/product-manage-form.mapper';
import type { NutritionMode, ProductFormData, ProductManagePrefill } from '../product-manage-lib/product-manage-form.types';
import { ProductNameSearchFacade } from '../product-manage-lib/product-name-search.facade';
import type { ProductNameSuggestion } from '../product-manage-lib/product-name-search.types';
import {
    buildOpenFoodFactsLookupPatch,
    buildResetNutritionPatch,
    buildSourceProductPrefillPatch,
    buildUsdaFoodDetailPrefillPatch,
} from '../product-manage-lib/product-nutrition-prefill.mapper';
import { ProductNutritionEditorComponent } from '../product-nutrition-editor/product-nutrition-editor.component';

export const VALIDATION_ERRORS_PROVIDER: FactoryProvider = {
    provide: FD_VALIDATION_ERRORS,
    useFactory: (): FdValidationErrors => ({
        required: () => 'FORM_ERRORS.REQUIRED',
        min: (error?: unknown) => ({
            key: 'FORM_ERRORS.INVALID_MIN_AMOUNT_MUST_BE_MORE_ZERO',
            params: { min: getNumberProperty(error, 'min') },
        }),
    }),
};

@Component({
    selector: 'fd-product-manage-form',
    templateUrl: './product-manage-form.component.html',
    styleUrls: ['./product-manage-form.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [VALIDATION_ERRORS_PROVIDER, ProductNameSearchFacade],
    imports: [
        ReactiveFormsModule,
        TranslatePipe,
        FdUiHintDirective,
        FdUiButtonComponent,
        FdUiFormErrorComponent,
        FdPageContainerDirective,
        ManageHeaderComponent,
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
    private readonly openFoodFactsService = inject(OpenFoodFactsService);
    private readonly usdaService = inject(UsdaService);

    public readonly product = input<Product | null>(null);
    public readonly prefill = input<ProductManagePrefill | null>(null);
    public readonly skipConfirmDialog = input(false);
    public readonly cancelMode = input<'emit' | 'navigate'>('navigate');
    public readonly saved = output<Product>();
    public readonly cancelled = output();
    public readonly globalError = signal<string | null>(null);
    private populatedProduct: Product | null = null;
    private appliedPrefillKey: string | null = null;
    private usdaDetailRequestId = 0;
    public readonly isDeleting = signal(false);
    public readonly isSubmitting = signal(false);

    public productForm: FormGroup<ProductFormData>;
    public nutritionMode: NutritionMode = 'base';

    public constructor() {
        this.productForm = createProductForm();
        this.bindFormEffects();
    }

    private bindFormEffects(): void {
        this.productForm.controls.baseUnit.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(unit => {
            this.productForm.controls.baseAmount.setValue(getDefaultProductBaseAmount(unit));
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
        this.productForm.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.clearGlobalError();
        });
    }

    private applyPrefillIfNeeded(prefill: ProductManagePrefill | null): void {
        const key = this.getPrefillKey(prefill);
        if (key === null || key === this.appliedPrefillKey || this.productForm.dirty) {
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

        this.productForm.controls.barcode.setValue(barcode);
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

    public readonly canShowDeleteButton = computed(() => {
        const currentProduct = this.product();
        return currentProduct?.isOwnedByCurrentUser === true;
    });
    public readonly manageHeaderState = computed<ProductManageHeaderState>(() => {
        const isEdit = this.product() !== null;

        return {
            titleKey: isEdit ? 'PRODUCT_MANAGE.EDIT_TITLE' : 'PRODUCT_MANAGE.ADD_TITLE',
            submitIcon: isEdit ? 'save' : 'add',
            submitLabelKey: isEdit ? 'PRODUCT_MANAGE.SAVE_BUTTON' : 'PRODUCT_MANAGE.ADD_BUTTON',
        };
    });

    public openBarcodeScanner(): void {
        this.fdDialogService
            .open<BarcodeScannerComponent, null, string | null>(BarcodeScannerComponent, { size: 'lg' })
            .afterClosed()
            .subscribe(barcode => {
                if (barcode !== null && barcode !== undefined && barcode.length > 0) {
                    this.productForm.controls.barcode.setValue(barcode);
                    this.lookupOpenFoodFacts(barcode);
                }
            });
    }

    private lookupOpenFoodFacts(barcode: string): void {
        if (this.product() !== null) {
            return;
        }

        this.openFoodFactsService
            .searchByBarcode(barcode)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(offProduct => {
                if (offProduct === null || this.product() !== null || this.productForm.controls.barcode.value !== barcode) {
                    return;
                }

                this.productForm.patchValue(buildOpenFoodFactsLookupPatch(this.productForm.getRawValue(), offProduct));
            });
    }

    private prefillFromOffProduct(offProduct: OpenFoodFactsProduct | ProductNameSuggestion): void {
        this.productForm.patchValue(buildSourceProductPrefillPatch(offProduct));
    }

    private prefillFromUsdaFoodDetail(detail: UsdaFoodDetail): void {
        this.productForm.patchValue(buildUsdaFoodDetailPrefillPatch(detail));
    }

    public openAiRecognitionDialog(): void {
        if (!this.ensurePremiumAccess()) {
            return;
        }

        this.fdDialogService
            .open<ProductAiRecognitionDialogComponent, { initialDescription?: string | null }, ProductAiRecognitionResult | null>(
                ProductAiRecognitionDialogComponent,
                {
                    size: 'lg',
                    data: {
                        initialDescription: this.productForm.controls.description.value ?? null,
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

    public async onCancelAsync(): Promise<void> {
        if (this.cancelMode() === 'emit') {
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

    public async onDeleteProductAsync(): Promise<void> {
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

    public async onSubmitAsync(): Promise<Product | null> {
        if (this.isSubmitting() || this.isDeleting()) {
            return null;
        }

        this.productForm.markAllAsTouched();

        if (this.hasMacrosError() || !this.productForm.valid) {
            return null;
        }

        this.isSubmitting.set(true);
        const productData = buildProductData(this.productForm, this.nutritionMode);
        const product = this.product() ?? null;
        const nextUsdaFdcId = this.productForm.controls.usdaFdcId.value;
        const previousUsdaFdcId = product?.usdaFdcId ?? null;

        try {
            const result = await this.productManageFacade.submitProductAsync(
                product,
                productData,
                this.skipConfirmDialog(),
                async savedProduct => this.syncUsdaLinkAsync(savedProduct, nextUsdaFdcId, previousUsdaFdcId),
            );
            if (result.error !== null) {
                this.handleSubmitError(result.error);
            }
            if (result.product !== null) {
                this.saved.emit(result.product);
            }
            return result.product;
        } finally {
            this.isSubmitting.set(false);
        }
    }

    public onNutritionModeChange(nextMode: string): void {
        const resolvedMode: NutritionMode = nextMode === 'portion' ? 'portion' : 'base';
        if (resolvedMode === this.nutritionMode) {
            return;
        }

        const baseAmount = getDefaultProductBaseAmount(this.productForm.controls.baseUnit.value);
        const portionAmount = getProductControlNumberValue(this.productForm.controls.defaultPortionAmount);

        if (portionAmount > 0) {
            const factor = resolvedMode === 'portion' ? portionAmount / baseAmount : baseAmount / portionAmount;
            this.convertNutritionControls(factor);
        }

        this.nutritionMode = resolvedMode;
    }

    public onNameQueryChange(query: string): void {
        this.nameSearch.search(query);
    }

    public onNameSuggestionSelected(suggestion: ProductNameSuggestion): void {
        if (suggestion.source === 'usda') {
            const fdcId = suggestion.usdaFdcId;
            if (fdcId === null || fdcId === undefined) {
                return;
            }

            this.productForm.patchValue({
                name: suggestion.name,
                barcode: null,
                brand: null,
                usdaFdcId: fdcId,
            });
            this.productForm.patchValue(buildResetNutritionPatch());
            this.nameSearch.setSelectedSuggestion(suggestion);
            const requestId = ++this.usdaDetailRequestId;
            this.usdaService
                .getFoodDetail(fdcId)
                .pipe(
                    catchError(() => of<UsdaFoodDetail | null>(null)),
                    takeUntilDestroyed(this.destroyRef),
                )
                .subscribe(detail => {
                    if (detail !== null && requestId === this.usdaDetailRequestId && this.productForm.controls.usdaFdcId.value === fdcId) {
                        this.prefillFromUsdaFoodDetail(detail);
                    }
                });
            return;
        }

        this.prefillFromOffProduct(suggestion);
        this.productForm.controls.usdaFdcId.setValue(null);
        this.nameSearch.setSelectedSuggestion(suggestion);
    }

    private hasMacrosError(): boolean {
        const controls = [
            this.productForm.controls.proteinsPerBase,
            this.productForm.controls.fatsPerBase,
            this.productForm.controls.carbsPerBase,
            this.productForm.controls.alcoholPerBase,
        ];

        return checkMacrosError(controls);
    }

    private convertNutritionControls(factor: number): void {
        const patch = buildConvertedNutritionPatch(this.productForm, factor);

        if (Object.keys(patch).length > 0) {
            this.productForm.patchValue(patch);
        }
    }

    private populateForm(product: Product): void {
        this.productForm.patchValue(buildProductFormPatch(product));
        this.nutritionMode = 'base';
    }

    private applyAiResult(result: ProductAiRecognitionResult): void {
        this.productForm.patchValue(buildAiResultPatch(this.productForm, result));
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
            await firstValueFrom(this.usdaService.linkProduct(savedProduct.id, nextFdcId));
            return;
        }

        if (previousFdcId !== null) {
            await firstValueFrom(this.usdaService.unlinkProduct(savedProduct.id));
        }
    }

    private hasUnsavedChanges(): boolean {
        return this.productForm.dirty;
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
}

type ProductManageHeaderState = {
    titleKey: string;
    submitIcon: 'save' | 'add';
    submitLabelKey: string;
};
