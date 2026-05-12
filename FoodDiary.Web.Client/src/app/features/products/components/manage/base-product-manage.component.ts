import type { HttpErrorResponse } from '@angular/common/http';
import {
    ChangeDetectionStrategy,
    Component,
    computed,
    DestroyRef,
    effect,
    type FactoryProvider,
    inject,
    input,
    signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
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
import type { FdUiSegmentedToggleOption } from 'fd-ui-kit/segmented-toggle/fd-ui-segmented-toggle.component';
import type { FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select.component';
import { catchError, debounceTime, firstValueFrom, map, of, Subject, switchMap } from 'rxjs';

import { BarcodeScannerComponent } from '../../../../components/shared/barcode-scanner/barcode-scanner.component';
import type { ConfirmDeleteDialogData } from '../../../../components/shared/confirm-delete-dialog/confirm-delete-dialog.component';
import { ManageHeaderComponent } from '../../../../components/shared/manage-header/manage-header.component';
import { FdPageContainerDirective } from '../../../../directives/layout/page-container.directive';
import { NavigationService } from '../../../../services/navigation.service';
import type { FormGroupControls } from '../../../../shared/lib/common.data';
import { NutritionCalculationService } from '../../../../shared/lib/nutrition-calculation.service';
import {
    calculateCalorieMismatchWarning,
    calculateMacroBarState,
    checkCaloriesError,
    checkMacrosError,
    getControlNumericValue,
} from '../../../../shared/lib/nutrition-form.utils';
import { getRecordProperty, getStringProperty, isRecord } from '../../../../shared/lib/unknown-value.utils';
import type { ImageSelection } from '../../../../shared/models/image-upload.data';
import { UsdaService } from '../../../usda/api/usda.service';
import type { Micronutrient, UsdaFoodDetail } from '../../../usda/models/usda.data';
import { type OpenFoodFactsProduct, OpenFoodFactsService } from '../../api/open-food-facts.service';
import { ProductService } from '../../api/product.service';
import {
    ProductAiRecognitionDialogComponent,
    type ProductAiRecognitionResult,
} from '../../dialogs/product-ai-recognition-dialog/product-ai-recognition-dialog.component';
import { ProductManageFacade } from '../../lib/product-manage.facade';
import { normalizeProductType as normalizeProductTypeValue } from '../../lib/product-type.utils';
import {
    type CreateProductRequest,
    MeasurementUnit,
    type Product,
    type ProductSearchSuggestion,
    ProductType,
    ProductVisibility,
} from '../../models/product.data';
import {
    ProductBasicInfoComponent,
    type ProductNameAutocompleteOption,
    type ProductNameSuggestion,
} from './product-basic-info/product-basic-info.component';
import { ProductNutritionEditorComponent } from './product-nutrition-editor/product-nutrition-editor.component';

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

const CALORIE_MISMATCH_THRESHOLD = 0.2;
const NAME_SEARCH_DEBOUNCE_MS = 600;
const NAME_SEARCH_MIN_LENGTH = 3;
const NAME_SEARCH_SUGGESTION_LIMIT = 5;
const DEFAULT_BASE_AMOUNT = 100;
const MIN_PRODUCT_AMOUNT = 0.001;
const NUTRIENT_ROUNDING_FACTOR = 10;
const KJ_TO_KCAL_FACTOR = 0.239005736;
const USDA_ENERGY_NUTRIENT_ID = 1008;
const USDA_PROTEIN_NUTRIENT_ID = 1003;
const USDA_FAT_NUTRIENT_ID = 1004;
const USDA_CARBS_NUTRIENT_ID = 1005;
const USDA_FIBER_NUTRIENT_ID = 1079;
const USDA_ALCOHOL_NUTRIENT_ID = 1018;
const HTTP_STATUS_UNAUTHORIZED = 401;
const HTTP_STATUS_BAD_REQUEST = 400;

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
    private readonly productService = inject(ProductService);
    private readonly usdaService = inject(UsdaService);

    public readonly product = input<Product | null>();
    public readonly globalError = signal<string | null>(null);
    public readonly nutritionWarning = signal<CalorieMismatchWarning | null>(null);
    public readonly macroBarState = signal<MacroBarState>({ isEmpty: true, segments: [] });
    private formInitialized = false;
    public readonly isDeleting = signal(false);
    public readonly isSubmitting = signal(false);

    protected skipConfirmDialog = false;
    public productForm: FormGroup<ProductFormData>;
    public unitOptions: Array<FdUiSelectOption<MeasurementUnit>> = [];
    public productTypeSelectOptions: Array<FdUiSelectOption<ProductType>> = [];
    public visibilitySelectOptions: Array<FdUiSelectOption<ProductVisibility>> = [];
    public nutritionMode: NutritionMode = 'base';
    public nutritionModeOptions: FdUiSegmentedToggleOption[] = [];
    public readonly nameOptions = signal<ProductNameAutocompleteOption[]>([]);
    public readonly isNameSearchLoading = signal(false);
    public readonly nutritionControlNames = {
        calories: 'caloriesPerBase',
        proteins: 'proteinsPerBase',
        fats: 'fatsPerBase',
        carbs: 'carbsPerBase',
        fiber: 'fiberPerBase',
        alcohol: 'alcoholPerBase',
    };
    private readonly nameQuery$ = new Subject<string>();
    private readonly nutritionFields: NutritionField[] = [
        'caloriesPerBase',
        'proteinsPerBase',
        'fatsPerBase',
        'carbsPerBase',
        'fiberPerBase',
        'alcoholPerBase',
    ];

    public readonly getControlErrorFn = (controlName: keyof ProductFormData): string | null => {
        return this.getControlError(controlName);
    };

    public constructor() {
        this.productForm = this.createProductForm();
        this.initializeFormOptions();
        this.bindFormEffects();
        this.bindNameSearch();
        this.prefillFromNavigationState();
    }

    private createProductForm(): FormGroup<ProductFormData> {
        return new FormGroup<ProductFormData>({
            name: new FormControl('', { nonNullable: true, validators: Validators.required }),
            barcode: new FormControl(null),
            brand: new FormControl(null),
            productType: new FormControl<ProductType>(ProductType.Unknown, { nonNullable: true }),
            description: new FormControl(null),
            comment: new FormControl(null),
            imageUrl: new FormControl<ImageSelection | null>(null),
            baseAmount: new FormControl(DEFAULT_BASE_AMOUNT, {
                nonNullable: true,
                validators: [Validators.required, Validators.min(MIN_PRODUCT_AMOUNT)],
            }),
            defaultPortionAmount: new FormControl(DEFAULT_BASE_AMOUNT, {
                nonNullable: true,
                validators: [Validators.required, Validators.min(MIN_PRODUCT_AMOUNT)],
            }),
            baseUnit: new FormControl(MeasurementUnit.G, { nonNullable: true, validators: Validators.required }),
            caloriesPerBase: new FormControl(null, [Validators.required, Validators.min(0)]),
            proteinsPerBase: new FormControl(null, [Validators.min(0)]),
            fatsPerBase: new FormControl(null, [Validators.min(0)]),
            carbsPerBase: new FormControl(null, [Validators.min(0)]),
            fiberPerBase: new FormControl(null, [Validators.min(0)]),
            alcoholPerBase: new FormControl(null, [Validators.min(0)]),
            visibility: new FormControl(ProductVisibility.Private, { nonNullable: true, validators: Validators.required }),
            usdaFdcId: new FormControl<number | null>(null),
        });
    }

    private initializeFormOptions(): void {
        this.buildUnitOptions();
        this.buildProductTypeOptions();
        this.buildVisibilityOptions();
        this.buildNutritionModeOptions();
    }

    private bindFormEffects(): void {
        this.productForm.controls.baseUnit.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(unit => {
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
            if (currentProduct !== null && currentProduct !== undefined && !this.formInitialized) {
                this.populateForm(currentProduct);
                this.formInitialized = true;
            }
        });
        this.productForm.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.clearGlobalError();
            this.updateCalorieWarning();
            this.updateMacroDistribution();
        });
    }

    private bindNameSearch(): void {
        this.nameQuery$
            .pipe(
                debounceTime(NAME_SEARCH_DEBOUNCE_MS),
                switchMap(query => {
                    const trimmed = query.trim();
                    if (trimmed.length < NAME_SEARCH_MIN_LENGTH) {
                        this.isNameSearchLoading.set(false);
                        this.nameOptions.set([]);
                        return of<ProductNameAutocompleteOption[]>([]);
                    }

                    this.isNameSearchLoading.set(true);
                    return this.productService
                        .searchSuggestions(trimmed, NAME_SEARCH_SUGGESTION_LIMIT)
                        .pipe(map(suggestions => suggestions.map(suggestion => this.toProductNameOption(suggestion))))
                        .pipe(catchError(() => of<ProductNameAutocompleteOption[]>([])));
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(options => {
                this.nameOptions.set(options);
                this.isNameSearchLoading.set(false);
            });
    }

    private prefillFromNavigationState(): void {
        const currentProduct = this.product();
        if (currentProduct === null || currentProduct === undefined) {
            const navigationState: unknown = history.state;
            const offProduct = getRecordProperty(navigationState, 'offProduct');
            if (this.isOpenFoodFactsProduct(offProduct)) {
                this.prefillFromOffProduct(offProduct);
            } else {
                const barcode = getStringProperty(navigationState, 'barcode');
                if (barcode !== undefined && barcode.length > 0) {
                    this.productForm.controls.barcode.setValue(barcode);
                    this.lookupOpenFoodFacts(barcode);
                }
            }
        }
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
                if (offProduct === null) {
                    return;
                }

                const controls = this.productForm.controls;
                if (controls.name.value.length === 0) {
                    controls.name.setValue(offProduct.name);
                }
                if (
                    (controls.brand.value === null || controls.brand.value.length === 0) &&
                    offProduct.brand !== null &&
                    offProduct.brand !== undefined &&
                    offProduct.brand.length > 0
                ) {
                    controls.brand.setValue(offProduct.brand);
                }
                this.prefillNutritionFromProduct(offProduct, false);
            });
    }

    private prefillFromOffProduct(offProduct: OpenFoodFactsProduct | ProductSearchSuggestion): void {
        const controls = this.productForm.controls;
        if (offProduct.barcode !== undefined && offProduct.barcode !== null && offProduct.barcode.length > 0) {
            controls.barcode.setValue(offProduct.barcode);
        }
        if (offProduct.name.length > 0) {
            controls.name.setValue(offProduct.name);
        }
        if (offProduct.brand !== null && offProduct.brand !== undefined && offProduct.brand.length > 0) {
            controls.brand.setValue(offProduct.brand);
        }
        this.prefillNutritionFromProduct(offProduct, true);
    }

    private prefillNutritionFromProduct(offProduct: OpenFoodFactsProduct | ProductSearchSuggestion, overwrite: boolean): void {
        const controls = this.productForm.controls;
        this.setNutritionControl(controls.caloriesPerBase, offProduct.caloriesPer100G, overwrite, true);
        this.setNutritionControl(controls.proteinsPerBase, offProduct.proteinsPer100G, overwrite);
        this.setNutritionControl(controls.fatsPerBase, offProduct.fatsPer100G, overwrite);
        this.setNutritionControl(controls.carbsPerBase, offProduct.carbsPer100G, overwrite);
        this.setNutritionControl(controls.fiberPerBase, offProduct.fiberPer100G, overwrite);
    }

    private setNutritionControl(
        control: FormControl<number | null>,
        value: number | null | undefined,
        overwrite: boolean,
        whole = false,
    ): void {
        if (value === null || value === undefined) {
            return;
        }

        if (!overwrite && control.value !== null) {
            return;
        }

        control.setValue(whole ? Math.round(value) : this.roundValue(value));
    }

    private prefillFromUsdaFoodDetail(detail: UsdaFoodDetail): void {
        const controls = this.productForm.controls;
        const nutrients = detail.nutrients;
        const calories = this.getUsdaNutrientAmount(nutrients, [USDA_ENERGY_NUTRIENT_ID], [/^energy$/i]);
        const proteins = this.getUsdaNutrientAmount(nutrients, [USDA_PROTEIN_NUTRIENT_ID], [/^protein$/i]);
        const fats = this.getUsdaNutrientAmount(nutrients, [USDA_FAT_NUTRIENT_ID], [/total lipid/i, /^fat$/i]);
        const carbs = this.getUsdaNutrientAmount(nutrients, [USDA_CARBS_NUTRIENT_ID], [/carbohydrate/i]);
        const fiber = this.getUsdaNutrientAmount(nutrients, [USDA_FIBER_NUTRIENT_ID], [/fiber/i]);
        const alcohol = this.getUsdaNutrientAmount(nutrients, [USDA_ALCOHOL_NUTRIENT_ID], [/alcohol/i]);

        controls.name.setValue(detail.description);
        controls.usdaFdcId.setValue(detail.fdcId);
        controls.baseUnit.setValue(MeasurementUnit.G);
        controls.baseAmount.setValue(DEFAULT_BASE_AMOUNT);

        if (calories !== null) {
            controls.caloriesPerBase.setValue(Math.round(calories));
        }
        if (proteins !== null) {
            controls.proteinsPerBase.setValue(this.roundValue(proteins));
        }
        if (fats !== null) {
            controls.fatsPerBase.setValue(this.roundValue(fats));
        }
        if (carbs !== null) {
            controls.carbsPerBase.setValue(this.roundValue(carbs));
        }
        if (fiber !== null) {
            controls.fiberPerBase.setValue(this.roundValue(fiber));
        }
        if (alcohol !== null) {
            controls.alcoholPerBase.setValue(this.roundValue(alcohol));
        }
    }

    private getUsdaNutrientAmount(nutrients: Micronutrient[], nutrientIds: number[], namePatterns: RegExp[]): number | null {
        const nutrient =
            nutrients.find(item => nutrientIds.includes(item.nutrientId)) ??
            nutrients.find(item => namePatterns.some(pattern => pattern.test(item.name)));

        if (nutrient === undefined) {
            return null;
        }

        return nutrient.unit.toLowerCase() === 'kj' ? nutrient.amountPer100g * KJ_TO_KCAL_FACTOR : nutrient.amountPer100g;
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
        if (this.hasUnsavedChanges()) {
            const shouldLeave = await this.confirmDiscardChangesAsync();
            if (shouldLeave !== true) {
                return;
            }
        }

        await this.navigationService.navigateToProductListAsync();
    }

    public async onDeleteProductAsync(): Promise<void> {
        const currentProduct = this.product();

        if (
            currentProduct === null ||
            currentProduct === undefined ||
            !currentProduct.isOwnedByCurrentUser ||
            this.isDeleting() ||
            this.isSubmitting()
        ) {
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
        this.productForm.markAllAsTouched();

        if (this.macrosError() !== null || !this.productForm.valid) {
            return null;
        }

        this.isSubmitting.set(true);
        const productData = this.buildProductData();
        const product = this.product() ?? null;
        const nextUsdaFdcId = this.productForm.controls.usdaFdcId.value;
        const previousUsdaFdcId = product?.usdaFdcId ?? null;

        try {
            const result = await this.productManageFacade.submitProductAsync(
                product,
                productData,
                this.skipConfirmDialog,
                async savedProduct => this.syncUsdaLinkAsync(savedProduct, nextUsdaFdcId, previousUsdaFdcId),
            );
            if (result.error !== null) {
                this.handleSubmitError(result.error);
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

        const baseAmount = this.getDefaultBaseAmount(this.productForm.controls.baseUnit.value);
        const portionAmount = this.getNumberValue(this.productForm.controls.defaultPortionAmount);

        if (portionAmount > 0) {
            const factor = resolvedMode === 'portion' ? portionAmount / baseAmount : baseAmount / portionAmount;
            this.convertNutritionControls(factor);
        }

        this.nutritionMode = resolvedMode;
    }

    private buildProductData(): CreateProductRequest {
        const controls = this.productForm.controls;
        const baseAmount = this.getDefaultBaseAmount(this.productForm.controls.baseUnit.value);
        const defaultPortionAmount = this.getNumberValue(this.productForm.controls.defaultPortionAmount);
        const normalizeFactor = this.getNutritionNormalizeFactor(baseAmount, defaultPortionAmount);
        const nutritionValues = this.getNormalizedNutritionValues(normalizeFactor);
        const imageSelection = controls.imageUrl.value;
        const productType = controls.productType.value;

        return {
            name: controls.name.value,
            barcode: controls.barcode.value,
            brand: controls.brand.value,
            productType,
            category: productType,
            description: controls.description.value,
            comment: controls.comment.value,
            imageUrl: imageSelection?.url ?? null,
            imageAssetId: imageSelection?.assetId ?? null,
            baseAmount,
            defaultPortionAmount,
            baseUnit: controls.baseUnit.value,
            ...nutritionValues,
            visibility: controls.visibility.value,
        };
    }

    private getNutritionNormalizeFactor(baseAmount: number, defaultPortionAmount: number): number {
        return this.nutritionMode === 'portion' && defaultPortionAmount > 0 ? baseAmount / defaultPortionAmount : 1;
    }

    private getNormalizedNutritionValues(normalizeFactor: number): ProductNutritionValues {
        return {
            caloriesPerBase: this.roundValue(this.getNumberValue(this.productForm.controls.caloriesPerBase) * normalizeFactor),
            proteinsPerBase: this.roundValue(this.getNumberValue(this.productForm.controls.proteinsPerBase) * normalizeFactor),
            fatsPerBase: this.roundValue(this.getNumberValue(this.productForm.controls.fatsPerBase) * normalizeFactor),
            carbsPerBase: this.roundValue(this.getNumberValue(this.productForm.controls.carbsPerBase) * normalizeFactor),
            fiberPerBase: this.roundValue(this.getNumberValue(this.productForm.controls.fiberPerBase) * normalizeFactor),
            alcoholPerBase: this.roundValue(this.getNumberValue(this.productForm.controls.alcoholPerBase) * normalizeFactor),
        };
    }

    public onNameQueryChange(query: string): void {
        this.nameQuery$.next(query);
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
            this.nameOptions.set([this.toProductNameOption(suggestion)]);
            this.usdaService
                .getFoodDetail(fdcId)
                .pipe(
                    catchError(() => of<UsdaFoodDetail | null>(null)),
                    takeUntilDestroyed(this.destroyRef),
                )
                .subscribe(detail => {
                    if (detail !== null) {
                        this.prefillFromUsdaFoodDetail(detail);
                    }
                });
            return;
        }

        this.prefillFromOffProduct(suggestion);
        this.productForm.controls.usdaFdcId.setValue(null);
        this.nameOptions.set([this.toProductNameOption(suggestion)]);
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

        if (!control.touched && !control.dirty) {
            return null;
        }

        const errors = control.errors;

        if (errors === null) {
            return null;
        }

        if (errors['required'] !== undefined) {
            return this.translateService.instant('FORM_ERRORS.REQUIRED');
        }

        const min = getNumberProperty(errors['min'], 'min');
        if (min !== undefined) {
            return this.translateService.instant('FORM_ERRORS.INVALID_MIN_AMOUNT_MUST_BE_MORE_ZERO', {
                min,
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
        this.nutritionWarning.set(
            calculateCalorieMismatchWarning({
                calories,
                proteins,
                fats,
                carbs,
                alcohol,
                threshold: CALORIE_MISMATCH_THRESHOLD,
            }),
        );
    }

    private updateMacroDistribution(): void {
        const proteins = this.getNumberValue(this.productForm.controls.proteinsPerBase);
        const fats = this.getNumberValue(this.productForm.controls.fatsPerBase);
        const carbs = this.getNumberValue(this.productForm.controls.carbsPerBase);
        this.macroBarState.set(calculateMacroBarState(proteins, fats, carbs));
    }

    private convertNutritionControls(factor: number): void {
        const patch: Partial<ProductFormValues> = {};
        this.nutritionFields.forEach(field => {
            const control = this.productForm.controls[field];
            const rawValue = control.value;
            if (rawValue === null) {
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
        if (sourceAmount === null || sourceAmount <= 0 || sourceAmount === targetAmount) {
            return this.roundNutritionValues(values, 1);
        }

        const factor = targetAmount / sourceAmount;

        return this.roundNutritionValues(values, factor);
    }

    private roundNutritionValues(values: NutritionValues, factor: number): NutritionValues {
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
        if (value === null) {
            return null;
        }
        return this.roundValue(value * factor);
    }

    private roundValue(value: number): number {
        return Math.round(value * NUTRIENT_ROUNDING_FACTOR) / NUTRIENT_ROUNDING_FACTOR;
    }

    private getUnitLabel(baseUnit: MeasurementUnit | null): string {
        if (baseUnit === null) {
            return '';
        }
        return this.translateService.instant(`GENERAL.UNITS.${baseUnit}`);
    }

    private getDefaultBaseAmount(unit: MeasurementUnit): number {
        return unit === MeasurementUnit.PCS ? 1 : DEFAULT_BASE_AMOUNT;
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
                imageUrl: this.getProductImageSelection(product),
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
                usdaFdcId: product.usdaFdcId ?? null,
            },
            { emitEvent: false },
        );
        this.nutritionMode = 'base';
        this.updateCalorieWarning();
        this.updateMacroDistribution();
    }

    private getProductImageSelection(product: Product): ImageSelection {
        return {
            url: product.imageUrl ?? null,
            assetId: product.imageAssetId ?? null,
        };
    }

    private applyAiResult(result: ProductAiRecognitionResult): void {
        const targetBaseAmount = this.getDefaultBaseAmount(result.baseUnit);
        const portionAmount = result.baseAmount > 0 ? result.baseAmount : targetBaseAmount;

        this.productForm.patchValue({
            name: result.name.length > 0 ? result.name : this.productForm.controls.name.value,
            description: result.description ?? this.productForm.controls.description.value,
            imageUrl: result.image ?? this.productForm.controls.imageUrl.value,
            baseAmount: targetBaseAmount,
            baseUnit: result.baseUnit,
            caloriesPerBase: this.roundNullableNutritionValue(result.caloriesPerBase),
            proteinsPerBase: this.roundNullableNutritionValue(result.proteinsPerBase),
            fatsPerBase: this.roundNullableNutritionValue(result.fatsPerBase),
            carbsPerBase: this.roundNullableNutritionValue(result.carbsPerBase),
            fiberPerBase: this.roundNullableNutritionValue(result.fiberPerBase),
            alcoholPerBase: this.roundNullableNutritionValue(result.alcoholPerBase),
            defaultPortionAmount: portionAmount,
        });

        this.nutritionMode = 'portion';
        this.buildNutritionModeOptions();
        this.updateCalorieWarning();
        this.updateMacroDistribution();
    }

    private handleSubmitError(error: HttpErrorResponse): void {
        if (error.status === HTTP_STATUS_UNAUTHORIZED) {
            this.setGlobalError('FORM_ERRORS.UNAUTHORIZED');
        } else if (error.status === HTTP_STATUS_BAD_REQUEST) {
            this.setGlobalError('FORM_ERRORS.INVALID_DATA');
        } else {
            this.setGlobalError('FORM_ERRORS.UNKNOWN');
        }
    }

    private setGlobalError(errorKey: string): void {
        this.globalError.set(this.translateService.instant(errorKey));
    }

    private roundNullableNutritionValue(value: number | null): number | null {
        return value === null ? null : this.roundValue(value);
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

    private isOpenFoodFactsProduct(value: unknown): value is OpenFoodFactsProduct {
        return isRecord(value) && typeof value['barcode'] === 'string' && typeof value['name'] === 'string';
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

    private toProductNameOption(suggestion: ProductSearchSuggestion): ProductNameAutocompleteOption {
        return {
            id:
                suggestion.source === 'usda'
                    ? `usda:${suggestion.usdaFdcId ?? suggestion.name}`
                    : `open-food-facts:${suggestion.barcode ?? suggestion.name}`,
            value: suggestion.name,
            label: suggestion.name,
            hint: suggestion.brand ?? suggestion.category ?? suggestion.barcode,
            badge: suggestion.source === 'usda' ? 'USDA' : 'Open Food Facts',
            data: suggestion,
        };
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
    usdaFdcId: number | null;
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

interface ProductNutritionValues {
    caloriesPerBase: number;
    proteinsPerBase: number;
    fatsPerBase: number;
    carbsPerBase: number;
    fiberPerBase: number;
    alcoholPerBase: number;
}

type NutritionField = keyof NutritionValues;

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

interface ProductManageHeaderState {
    titleKey: string;
    submitIcon: 'save' | 'add';
    submitLabelKey: string;
}
