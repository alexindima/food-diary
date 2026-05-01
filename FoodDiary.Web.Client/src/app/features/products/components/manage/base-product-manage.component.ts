import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, FactoryProvider, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FD_VALIDATION_ERRORS, FdUiFormErrorComponent, FdValidationErrors } from 'fd-ui-kit/form-error/fd-ui-form-error.component';
import { FdUiSegmentedToggleOption } from 'fd-ui-kit/segmented-toggle/fd-ui-segmented-toggle.component';
import { FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select.component';

import { BarcodeScannerComponent } from '../../../../components/shared/barcode-scanner/barcode-scanner.component';
import { ConfirmDeleteDialogData } from '../../../../components/shared/confirm-delete-dialog/confirm-delete-dialog.component';
import { ManageHeaderComponent } from '../../../../components/shared/manage-header/manage-header.component';
import { FdPageContainerDirective } from '../../../../directives/layout/page-container.directive';
import { NavigationService } from '../../../../services/navigation.service';
import { FormGroupControls } from '../../../../shared/lib/common.data';
import { NutritionCalculationService } from '../../../../shared/lib/nutrition-calculation.service';
import {
    calculateCalorieMismatchWarning,
    calculateMacroBarState,
    checkCaloriesError,
    checkMacrosError,
    getControlNumericValue,
} from '../../../../shared/lib/nutrition-form.utils';
import { ImageSelection } from '../../../../shared/models/image-upload.data';
import { OpenFoodFactsProduct, OpenFoodFactsService } from '../../api/open-food-facts.service';
import {
    ProductAiRecognitionDialogComponent,
    ProductAiRecognitionResult,
} from '../../dialogs/product-ai-recognition-dialog/product-ai-recognition-dialog.component';
import { ProductManageFacade } from '../../lib/product-manage.facade';
import { normalizeProductType as normalizeProductTypeValue } from '../../lib/product-type.utils';
import { CreateProductRequest, MeasurementUnit, Product, ProductType, ProductVisibility } from '../../models/product.data';
import { ProductBasicInfoComponent } from './product-basic-info/product-basic-info.component';
import { ProductNutritionEditorComponent } from './product-nutrition-editor/product-nutrition-editor.component';

export const VALIDATION_ERRORS_PROVIDER: FactoryProvider = {
    provide: FD_VALIDATION_ERRORS,
    useFactory: (): FdValidationErrors => ({
        required: () => 'FORM_ERRORS.REQUIRED',
        min: (error?: unknown) => ({
            key: 'FORM_ERRORS.INVALID_MIN_AMOUNT_MUST_BE_MORE_ZERO',
            params: { min: (error as { min?: number } | undefined)?.min },
        }),
    }),
};

@Component({
    selector: 'fd-base-product-manage',
    templateUrl: './base-product-manage.component.html',
    styleUrls: ['./base-product-manage.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [VALIDATION_ERRORS_PROVIDER],
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
export class BaseProductManageComponent {
    protected readonly translateService = inject(TranslateService);
    protected readonly navigationService = inject(NavigationService);
    protected readonly fdDialogService = inject(FdUiDialogService);
    private readonly nutritionCalculationService = inject(NutritionCalculationService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly productManageFacade = inject(ProductManageFacade);
    private readonly openFoodFactsService = inject(OpenFoodFactsService);

    public readonly product = input<Product | null>();
    public readonly globalError = signal<string | null>(null);
    public readonly nutritionWarning = signal<CalorieMismatchWarning | null>(null);
    public readonly macroBarState = signal<MacroBarState>({ isEmpty: true, segments: [] });
    private formInitialized = false;
    public readonly isDeleting = signal(false);
    public readonly isSubmitting = signal(false);
    private readonly calorieMismatchThreshold = 0.2;

    protected skipConfirmDialog = false;
    public productForm: FormGroup<ProductFormData>;
    public unitOptions: FdUiSelectOption<MeasurementUnit>[] = [];
    public productTypeSelectOptions: FdUiSelectOption<ProductType>[] = [];
    public visibilitySelectOptions: FdUiSelectOption<ProductVisibility>[] = [];
    public nutritionMode: NutritionMode = 'base';
    public nutritionModeOptions: FdUiSegmentedToggleOption[] = [];
    public readonly nutritionControlNames = {
        calories: 'caloriesPerBase',
        proteins: 'proteinsPerBase',
        fats: 'fatsPerBase',
        carbs: 'carbsPerBase',
        fiber: 'fiberPerBase',
        alcohol: 'alcoholPerBase',
    };

    public readonly getControlErrorFn = (controlName: keyof ProductFormData): string | null => {
        return this.getControlError(controlName);
    };

    public constructor() {
        this.productForm = new FormGroup<ProductFormData>({
            name: new FormControl('', { nonNullable: true, validators: Validators.required }),
            barcode: new FormControl(null),
            brand: new FormControl(null),
            productType: new FormControl<ProductType>(ProductType.Unknown, { nonNullable: true }),
            description: new FormControl(null),
            comment: new FormControl(null),
            imageUrl: new FormControl<ImageSelection | null>(null),
            baseAmount: new FormControl(100, { nonNullable: true, validators: [Validators.required, Validators.min(0.001)] }),
            defaultPortionAmount: new FormControl(100, {
                nonNullable: true,
                validators: [Validators.required, Validators.min(0.001)],
            }),
            baseUnit: new FormControl(MeasurementUnit.G, { nonNullable: true, validators: Validators.required }),
            caloriesPerBase: new FormControl(null, [Validators.required, Validators.min(0)]),
            proteinsPerBase: new FormControl(null, [Validators.min(0)]),
            fatsPerBase: new FormControl(null, [Validators.min(0)]),
            carbsPerBase: new FormControl(null, [Validators.min(0)]),
            fiberPerBase: new FormControl(null, [Validators.min(0)]),
            alcoholPerBase: new FormControl(null, [Validators.min(0)]),
            visibility: new FormControl(ProductVisibility.Private, { nonNullable: true, validators: Validators.required }),
        });

        this.buildUnitOptions();
        this.buildProductTypeOptions();
        this.buildVisibilityOptions();
        this.buildNutritionModeOptions();
        this.productForm.controls.baseUnit.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(unit => {
            if (!unit) {
                return;
            }
            this.productForm.controls.baseAmount.setValue(this.getDefaultBaseAmount(unit));
            this.buildNutritionModeOptions();
        });
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.buildUnitOptions();
            this.buildProductTypeOptions();
            this.buildVisibilityOptions();
            this.buildNutritionModeOptions();
        });

        effect(() => {
            const currentProduct = this.product();
            if (currentProduct && !this.formInitialized) {
                this.populateForm(currentProduct);
                this.formInitialized = true;
            }
        });
        this.productForm.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.clearGlobalError();
            this.updateCalorieWarning();
            this.updateMacroDistribution();
        });

        if (!this.product()) {
            const offProduct = history.state?.offProduct as OpenFoodFactsProduct | undefined;
            if (offProduct) {
                this.prefillFromOffProduct(offProduct);
            } else {
                const barcode = history.state?.barcode as string | undefined;
                if (barcode) {
                    this.productForm.controls.barcode.setValue(barcode);
                    this.lookupOpenFoodFacts(barcode);
                }
            }
        }
    }

    public readonly canShowDeleteButton = computed(() => {
        const currentProduct = this.product();
        return !!currentProduct && currentProduct.isOwnedByCurrentUser;
    });

    public openBarcodeScanner(): void {
        this.fdDialogService
            .open<BarcodeScannerComponent, null, string | null>(BarcodeScannerComponent, { size: 'lg' })
            .afterClosed()
            .subscribe(barcode => {
                if (barcode) {
                    this.productForm.controls.barcode.setValue(barcode);
                    this.lookupOpenFoodFacts(barcode);
                }
            });
    }

    private lookupOpenFoodFacts(barcode: string): void {
        if (this.product()) {
            return;
        }

        this.openFoodFactsService
            .searchByBarcode(barcode)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(offProduct => {
                if (!offProduct) {
                    return;
                }

                const controls = this.productForm.controls;
                if (!controls.name.value) {
                    controls.name.setValue(offProduct.name);
                }
                if (!controls.brand.value && offProduct.brand) {
                    controls.brand.setValue(offProduct.brand);
                }
                const cal = offProduct.caloriesPer100G;
                if (cal !== undefined && cal !== null && controls.caloriesPerBase.value === null) {
                    controls.caloriesPerBase.setValue(Math.round(cal));
                }
                const pro = offProduct.proteinsPer100G;
                if (pro !== undefined && pro !== null && controls.proteinsPerBase.value === null) {
                    controls.proteinsPerBase.setValue(Math.round(pro * 10) / 10);
                }
                const fat = offProduct.fatsPer100G;
                if (fat !== undefined && fat !== null && controls.fatsPerBase.value === null) {
                    controls.fatsPerBase.setValue(Math.round(fat * 10) / 10);
                }
                const carb = offProduct.carbsPer100G;
                if (carb !== undefined && carb !== null && controls.carbsPerBase.value === null) {
                    controls.carbsPerBase.setValue(Math.round(carb * 10) / 10);
                }
                const fib = offProduct.fiberPer100G;
                if (fib !== undefined && fib !== null && controls.fiberPerBase.value === null) {
                    controls.fiberPerBase.setValue(Math.round(fib * 10) / 10);
                }
            });
    }

    private prefillFromOffProduct(offProduct: OpenFoodFactsProduct): void {
        const controls = this.productForm.controls;
        if (offProduct.barcode) {
            controls.barcode.setValue(offProduct.barcode);
        }
        if (offProduct.name) {
            controls.name.setValue(offProduct.name);
        }
        if (offProduct.brand) {
            controls.brand.setValue(offProduct.brand);
        }
        const cal = offProduct.caloriesPer100G;
        if (cal !== undefined && cal !== null) {
            controls.caloriesPerBase.setValue(Math.round(cal));
        }
        const pro = offProduct.proteinsPer100G;
        if (pro !== undefined && pro !== null) {
            controls.proteinsPerBase.setValue(Math.round(pro * 10) / 10);
        }
        const fat = offProduct.fatsPer100G;
        if (fat !== undefined && fat !== null) {
            controls.fatsPerBase.setValue(Math.round(fat * 10) / 10);
        }
        const carb = offProduct.carbsPer100G;
        if (carb !== undefined && carb !== null) {
            controls.carbsPerBase.setValue(Math.round(carb * 10) / 10);
        }
        const fib = offProduct.fiberPer100G;
        if (fib !== undefined && fib !== null) {
            controls.fiberPerBase.setValue(Math.round(fib * 10) / 10);
        }
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
                if (!result) {
                    return;
                }

                this.applyAiResult(result);
            });
    }

    public async onCancel(): Promise<void> {
        if (this.hasUnsavedChanges()) {
            const shouldLeave = await this.confirmDiscardChanges();
            if (!shouldLeave) {
                return;
            }
        }

        await this.navigationService.navigateToProductList();
    }

    public async onDeleteProduct(): Promise<void> {
        const currentProduct = this.product();

        if (!currentProduct || !currentProduct.isOwnedByCurrentUser || this.isDeleting() || this.isSubmitting()) {
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

        const result = await this.productManageFacade.deleteProduct(currentProduct, data);
        if (result !== 'deleted') {
            this.isDeleting.set(false);
            if (result === 'error') {
                this.setGlobalError('PRODUCT_MANAGE.DELETE_ERROR');
            }
        }
    }

    public async onSubmit(): Promise<Product | null> {
        this.productForm.markAllAsTouched();

        if (this.macrosError()) {
            return null;
        }

        if (this.productForm.valid) {
            this.isSubmitting.set(true);
            const baseAmount = this.getDefaultBaseAmount(this.productForm.controls.baseUnit.value);
            const defaultPortionAmount = this.getNumberValue(this.productForm.controls.defaultPortionAmount);
            const normalizeFactor = this.nutritionMode === 'portion' && defaultPortionAmount > 0 ? baseAmount / defaultPortionAmount : 1;
            const caloriesPerBase = this.roundValue(this.getNumberValue(this.productForm.controls.caloriesPerBase) * normalizeFactor);
            const proteinsPerBase = this.roundValue(this.getNumberValue(this.productForm.controls.proteinsPerBase) * normalizeFactor);
            const fatsPerBase = this.roundValue(this.getNumberValue(this.productForm.controls.fatsPerBase) * normalizeFactor);
            const carbsPerBase = this.roundValue(this.getNumberValue(this.productForm.controls.carbsPerBase) * normalizeFactor);
            const fiberPerBase = this.roundValue(this.getNumberValue(this.productForm.controls.fiberPerBase) * normalizeFactor);
            const alcoholPerBase = this.roundValue(this.getNumberValue(this.productForm.controls.alcoholPerBase) * normalizeFactor);

            const productData: CreateProductRequest = {
                name: this.productForm.value.name!,
                barcode: this.productForm.value.barcode || null,
                brand: this.productForm.value.brand || null,
                productType: this.productForm.value.productType || ProductType.Unknown,
                category: this.productForm.value.productType || null,
                description: this.productForm.value.description || null,
                comment: this.productForm.value.comment || null,
                imageUrl: this.productForm.value.imageUrl?.url || null,
                imageAssetId: this.productForm.value.imageUrl?.assetId || null,
                baseAmount,
                defaultPortionAmount,
                baseUnit: this.productForm.value.baseUnit!,
                caloriesPerBase,
                proteinsPerBase,
                fatsPerBase,
                carbsPerBase,
                fiberPerBase,
                alcoholPerBase,
                visibility: this.productForm.value.visibility!,
            };
            const product = this.product();

            try {
                const result = await this.productManageFacade.submitProduct(product ?? null, productData, this.skipConfirmDialog);
                if (result.error) {
                    this.handleSubmitError(result.error);
                }
                return result.product;
            } finally {
                this.isSubmitting.set(false);
            }
        }

        return null;
    }

    public onNutritionModeChange(nextMode: string): void {
        const resolvedMode: NutritionMode = nextMode === 'portion' ? 'portion' : 'base';
        if (resolvedMode === this.nutritionMode) {
            return;
        }

        const baseAmount = this.getDefaultBaseAmount(this.productForm.controls.baseUnit.value);
        const portionAmount = this.getNumberValue(this.productForm.controls.defaultPortionAmount);

        if (portionAmount > 0) {
            const factor = resolvedMode === 'portion' ? portionAmount / baseAmount : baseAmount / portionAmount;
            this.convertNutritionControls(factor);
        }

        this.nutritionMode = resolvedMode;
    }

    public caloriesError(): string | null {
        const control = this.productForm.controls.caloriesPerBase;
        return checkCaloriesError(control) ? this.translateService.instant('PRODUCT_MANAGE.NUTRITION_ERRORS.CALORIES_REQUIRED') : null;
    }

    public macrosError(): string | null {
        const controls = [
            this.productForm.controls.proteinsPerBase,
            this.productForm.controls.fatsPerBase,
            this.productForm.controls.carbsPerBase,
            this.productForm.controls.alcoholPerBase,
        ];

        return checkMacrosError(controls) ? this.translateService.instant('PRODUCT_MANAGE.NUTRITION_ERRORS.MACROS_REQUIRED') : null;
    }

    public getControlError(controlName: keyof ProductFormData): string | null {
        const control = this.productForm.controls[controlName];

        if (!control || (!control.touched && !control.dirty)) {
            return null;
        }

        const errors = control.errors;

        if (!errors) {
            return null;
        }

        if (errors['required']) {
            return this.translateService.instant('FORM_ERRORS.REQUIRED');
        }

        if (errors['min']) {
            return this.translateService.instant('FORM_ERRORS.INVALID_MIN_AMOUNT_MUST_BE_MORE_ZERO', {
                min: errors['min'].min,
            });
        }

        return null;
    }

    private updateCalorieWarning(): void {
        const calories = this.getNumberValue(this.productForm.controls.caloriesPerBase);
        const proteins = this.getNumberValue(this.productForm.controls.proteinsPerBase);
        const fats = this.getNumberValue(this.productForm.controls.fatsPerBase);
        const carbs = this.getNumberValue(this.productForm.controls.carbsPerBase);
        const alcohol = this.getNumberValue(this.productForm.controls.alcoholPerBase);
        this.nutritionWarning.set(calculateCalorieMismatchWarning(calories, proteins, fats, carbs, alcohol, this.calorieMismatchThreshold));
    }

    private updateMacroDistribution(): void {
        const proteins = this.getNumberValue(this.productForm.controls.proteinsPerBase);
        const fats = this.getNumberValue(this.productForm.controls.fatsPerBase);
        const carbs = this.getNumberValue(this.productForm.controls.carbsPerBase);
        this.macroBarState.set(calculateMacroBarState(proteins, fats, carbs));
    }

    private convertNutritionControls(factor: number): void {
        const patch: Partial<ProductFormValues> = {};
        const fields: Array<
            keyof Pick<
                ProductFormValues,
                'caloriesPerBase' | 'proteinsPerBase' | 'fatsPerBase' | 'carbsPerBase' | 'fiberPerBase' | 'alcoholPerBase'
            >
        > = ['caloriesPerBase', 'proteinsPerBase', 'fatsPerBase', 'carbsPerBase', 'fiberPerBase', 'alcoholPerBase'];

        fields.forEach(field => {
            const control = this.productForm.controls[field];
            const rawValue = control.value as unknown;
            if (rawValue === null || rawValue === undefined || rawValue === '') {
                return;
            }
            const value = this.getNumberValue(control);
            patch[field] = this.roundValue(value * factor);
        });

        if (Object.keys(patch).length > 0) {
            this.productForm.patchValue(patch);
        }
    }

    private normalizeNutritionValues(values: NutritionValues, sourceAmount: number | null, targetAmount: number): NutritionValues {
        if (!sourceAmount || sourceAmount <= 0 || sourceAmount === targetAmount) {
            return values;
        }

        const factor = targetAmount / sourceAmount;

        return {
            caloriesPerBase: this.roundOptional(values.caloriesPerBase, factor),
            proteinsPerBase: this.roundOptional(values.proteinsPerBase, factor),
            fatsPerBase: this.roundOptional(values.fatsPerBase, factor),
            carbsPerBase: this.roundOptional(values.carbsPerBase, factor),
            fiberPerBase: this.roundOptional(values.fiberPerBase, factor),
            alcoholPerBase: this.roundOptional(values.alcoholPerBase, factor),
        };
    }

    private roundOptional(value: number | null, factor: number): number | null {
        if (value === null || value === undefined) {
            return null;
        }
        return this.roundValue(value * factor);
    }

    private roundValue(value: number): number {
        return Math.round(value * 1000) / 1000;
    }

    private getUnitLabel(baseUnit: MeasurementUnit | null): string {
        if (!baseUnit) {
            return '';
        }
        return this.translateService.instant(`GENERAL.UNITS.${baseUnit}`);
    }

    private getDefaultBaseAmount(unit: MeasurementUnit): number {
        return unit === MeasurementUnit.PCS ? 1 : 100;
    }

    private getNumberValue(control: FormControl<number | string | null>): number {
        return getControlNumericValue(control);
    }

    private populateForm(product: Product): void {
        const normalizedVisibility = this.normalizeVisibility(product.visibility);
        const normalizedProductType = normalizeProductTypeValue(product.productType ?? product.category ?? null) ?? ProductType.Unknown;
        const targetBaseAmount = this.getDefaultBaseAmount(product.baseUnit);
        const normalizedNutrition = this.normalizeNutritionValues(
            {
                caloriesPerBase: product.caloriesPerBase,
                proteinsPerBase: product.proteinsPerBase,
                fatsPerBase: product.fatsPerBase,
                carbsPerBase: product.carbsPerBase,
                fiberPerBase: product.fiberPerBase,
                alcoholPerBase: product.alcoholPerBase,
            },
            product.baseAmount,
            targetBaseAmount,
        );

        this.productForm.patchValue(
            {
                name: product.name,
                barcode: product.barcode ?? null,
                brand: product.brand ?? null,
                productType: normalizedProductType,
                description: product.description ?? null,
                comment: product.comment ?? null,
                imageUrl: {
                    url: product.imageUrl ?? null,
                    assetId: product.imageAssetId ?? null,
                },
                baseAmount: targetBaseAmount,
                defaultPortionAmount: product.defaultPortionAmount,
                baseUnit: product.baseUnit,
                caloriesPerBase: normalizedNutrition.caloriesPerBase,
                proteinsPerBase: normalizedNutrition.proteinsPerBase,
                fatsPerBase: normalizedNutrition.fatsPerBase,
                carbsPerBase: normalizedNutrition.carbsPerBase,
                fiberPerBase: normalizedNutrition.fiberPerBase,
                alcoholPerBase: normalizedNutrition.alcoholPerBase,
                visibility: normalizedVisibility,
            },
            { emitEvent: false },
        );
        this.nutritionMode = 'base';
        this.updateCalorieWarning();
        this.updateMacroDistribution();
    }

    private applyAiResult(result: ProductAiRecognitionResult): void {
        const defaultPortionControl = this.productForm.controls.defaultPortionAmount;
        const targetBaseAmount = this.getDefaultBaseAmount(result.baseUnit);
        const shouldUpdatePortion =
            defaultPortionControl.pristine ||
            defaultPortionControl.value === this.getDefaultBaseAmount(this.productForm.controls.baseUnit.value);
        const normalizedNutrition = this.normalizeNutritionValues(
            {
                caloriesPerBase: result.caloriesPerBase,
                proteinsPerBase: result.proteinsPerBase,
                fatsPerBase: result.fatsPerBase,
                carbsPerBase: result.carbsPerBase,
                fiberPerBase: result.fiberPerBase,
                alcoholPerBase: result.alcoholPerBase,
            },
            result.baseAmount,
            targetBaseAmount,
        );

        this.productForm.patchValue({
            name: result.name || this.productForm.controls.name.value,
            description: result.description ?? this.productForm.controls.description.value,
            baseAmount: targetBaseAmount,
            baseUnit: result.baseUnit,
            caloriesPerBase: normalizedNutrition.caloriesPerBase,
            proteinsPerBase: normalizedNutrition.proteinsPerBase,
            fatsPerBase: normalizedNutrition.fatsPerBase,
            carbsPerBase: normalizedNutrition.carbsPerBase,
            fiberPerBase: normalizedNutrition.fiberPerBase,
            alcoholPerBase: normalizedNutrition.alcoholPerBase,
            defaultPortionAmount: shouldUpdatePortion ? targetBaseAmount : defaultPortionControl.value,
        });

        if (this.nutritionMode === 'portion') {
            const portionAmount = this.getNumberValue(defaultPortionControl);
            if (portionAmount > 0) {
                this.convertNutritionControls(portionAmount / targetBaseAmount);
            }
        }
        this.updateMacroDistribution();
    }

    private handleSubmitError(error: HttpErrorResponse): void {
        if (error.status === 401) {
            this.setGlobalError('FORM_ERRORS.UNAUTHORIZED');
        } else if (error.status === 400) {
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

    private buildVisibilityOptions(): void {
        this.visibilitySelectOptions = (Object.values(ProductVisibility) as ProductVisibility[]).map(option => ({
            value: option,
            label: this.translateService.instant(`PRODUCT_MANAGE.VISIBILITY_OPTIONS.${option.toUpperCase()}`),
        }));
    }

    private buildUnitOptions(): void {
        this.unitOptions = (Object.values(MeasurementUnit) as MeasurementUnit[]).map(unit => ({
            value: unit,
            label: this.translateService.instant(`PRODUCT_AMOUNT_UNITS.${MeasurementUnit[unit]}`),
        }));
    }

    private buildProductTypeOptions(): void {
        this.productTypeSelectOptions = (Object.values(ProductType) as ProductType[]).map(type => ({
            value: type,
            label: this.translateService.instant(`PRODUCT_MANAGE.PRODUCT_TYPE_OPTIONS.${type.toUpperCase()}`),
        }));
    }

    private buildNutritionModeOptions(): void {
        const baseUnit = this.productForm.controls.baseUnit.value;
        const amount = this.getDefaultBaseAmount(baseUnit);
        const unitLabel = this.getUnitLabel(baseUnit);

        this.nutritionModeOptions = [
            {
                value: 'base',
                label: this.translateService.instant('PRODUCT_MANAGE.NUTRITION_MODE.BASE', {
                    amount,
                    unit: unitLabel,
                }),
            },
            {
                value: 'portion',
                label: this.translateService.instant('PRODUCT_MANAGE.NUTRITION_MODE.PORTION'),
            },
        ];
    }

    private normalizeVisibility(value: ProductVisibility | null | string | undefined): ProductVisibility {
        if (typeof value !== 'string') {
            return ProductVisibility.Private;
        }

        const upper = value.toUpperCase();
        return upper === ProductVisibility.Public.toUpperCase() ? ProductVisibility.Public : ProductVisibility.Private;
    }

    private hasUnsavedChanges(): boolean {
        return this.productForm.dirty;
    }

    private async confirmDiscardChanges(): Promise<boolean> {
        const data: ConfirmDeleteDialogData = {
            title: this.translateService.instant('PRODUCT_MANAGE.LEAVE_CONFIRM_TITLE'),
            message: this.translateService.instant('PRODUCT_MANAGE.LEAVE_CONFIRM_MESSAGE'),
            confirmLabel: this.translateService.instant('PRODUCT_MANAGE.LEAVE_CONFIRM_BUTTON'),
            cancelLabel: this.translateService.instant('PRODUCT_MANAGE.LEAVE_STAY_BUTTON'),
        };
        return this.productManageFacade.confirmDiscardChanges(data);
    }

    private ensurePremiumAccess(): boolean {
        return this.productManageFacade.ensurePremiumAccess();
    }

    protected readonly MeasurementUnit = MeasurementUnit;
    protected readonly ProductType = ProductType;
}

export interface ProductFormValues {
    name: string;
    barcode: string | null;
    brand: string | null;
    productType: ProductType;
    description: string | null;
    comment: string | null;
    imageUrl: ImageSelection | null;
    baseAmount: number;
    defaultPortionAmount: number;
    baseUnit: MeasurementUnit;
    caloriesPerBase: number | null;
    proteinsPerBase: number | null;
    fatsPerBase: number | null;
    carbsPerBase: number | null;
    fiberPerBase: number | null;
    alcoholPerBase: number | null;
    visibility: ProductVisibility;
}

export type NutritionMode = 'base' | 'portion';

interface NutritionValues {
    caloriesPerBase: number | null;
    proteinsPerBase: number | null;
    fatsPerBase: number | null;
    carbsPerBase: number | null;
    fiberPerBase: number | null;
    alcoholPerBase: number | null;
}

type MacroKey = 'proteins' | 'fats' | 'carbs';

interface MacroBarSegment {
    key: MacroKey;
    percent: number;
}

interface MacroBarState {
    isEmpty: boolean;
    segments: MacroBarSegment[];
}

export type ProductFormData = FormGroupControls<ProductFormValues>;

interface CalorieMismatchWarning {
    expectedCalories: number;
    actualCalories: number;
}
