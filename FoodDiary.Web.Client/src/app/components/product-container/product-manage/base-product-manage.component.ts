import {
    ChangeDetectionStrategy,
    Component,
    DestroyRef,
    FactoryProvider,
    effect,
    inject,
    input,
    OnInit,
    signal,
} from '@angular/core';
import { Product, CreateProductRequest, MeasurementUnit, ProductVisibility, ProductType } from '../../../types/product.data';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ProductService } from '../../../services/product.service';
import { NavigationService } from '../../../services/navigation.service';
import { HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormGroupControls } from '../../../types/common.data';
import { firstValueFrom } from 'rxjs';
import { ZXingScannerModule } from '@zxing/ngx-scanner';
import { BarcodeScannerComponent } from '../../shared/barcode-scanner/barcode-scanner.component';
import { FdUiFormErrorComponent, FD_VALIDATION_ERRORS, FdValidationErrors } from 'fd-ui-kit/form-error/fd-ui-form-error.component';
import { FdUiPlainInputComponent } from 'fd-ui-kit/plain-input/fd-ui-plain-input.component';
import { FdUiPlainTextareaComponent } from 'fd-ui-kit/plain-textarea/fd-ui-plain-textarea.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiSelectComponent, FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select.component';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiNutrientInputComponent } from 'fd-ui-kit/nutrient-input/fd-ui-nutrient-input.component';
import { normalizeProductType as normalizeProductTypeValue } from '../../../utils/product-type.utils';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import {
    ProductSaveSuccessDialogComponent,
    ProductSaveSuccessDialogData,
} from './product-save-success-dialog.component';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { FdPageContainerDirective } from '../../../directives/layout/page-container.directive';
import { NutritionCalculationService } from '../../../services/nutrition-calculation.service';
import {
    ConfirmDeleteDialogComponent,
    ConfirmDeleteDialogData,
} from '../../shared/confirm-delete-dialog/confirm-delete-dialog.component';
import { ImageUploadFieldComponent } from '../../shared/image-upload-field/image-upload-field.component';
import { ImageSelection } from '../../../types/image-upload.data';

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
        ZXingScannerModule,
        FdUiPlainInputComponent,
        FdUiPlainTextareaComponent,
        FdUiCardComponent,
        FdUiSelectComponent,
        FdUiButtonComponent,
        FdUiNutrientInputComponent,
        FdUiFormErrorComponent,
        PageHeaderComponent,
        FdPageContainerDirective,
        ImageUploadFieldComponent,
    ],
})
export class BaseProductManageComponent implements OnInit {
    protected readonly productService = inject(ProductService);
    protected readonly translateService = inject(TranslateService);
    protected readonly navigationService = inject(NavigationService);
    protected readonly fdDialogService = inject(FdUiDialogService);
    private readonly nutritionCalculationService = inject(NutritionCalculationService);
    private readonly destroyRef = inject(DestroyRef);

    public product = input<Product | null>();
    public globalError = signal<string | null>(null);
    public nutritionWarning = signal<CalorieMismatchWarning | null>(null);
    private formInitialized = false;
    public readonly isDeleting = signal(false);
    private readonly calorieMismatchThreshold = 0.2;

    protected skipConfirmDialog = false;
    public productForm: FormGroup<ProductFormData>;
    public units = Object.values(MeasurementUnit) as MeasurementUnit[];
    public unitOptions: FdUiSelectOption<MeasurementUnit>[] = [];
    public productTypes = Object.values(ProductType) as ProductType[];
    public productTypeSelectOptions: FdUiSelectOption<ProductType>[] = [];
    public visibilityOptions = Object.values(ProductVisibility) as ProductVisibility[];
    public visibilitySelectOptions: FdUiSelectOption<ProductVisibility>[] = [];
    private readonly nutrientFillAlpha = 0.14;
    private readonly nutrientPalette = {
        calories: '#E11D48',
        proteins: '#0284C7',
        fats: '#C2410C',
        carbs: '#0F766E',
        fiber: '#7E22CE',
        alcohol: '#64748B',
    };
    public readonly nutrientFillColors = {
        calories: this.applyAlpha(this.nutrientPalette.calories, this.nutrientFillAlpha),
        fiber: this.applyAlpha(this.nutrientPalette.fiber, this.nutrientFillAlpha),
        proteins: this.applyAlpha(this.nutrientPalette.proteins, this.nutrientFillAlpha),
        fats: this.applyAlpha(this.nutrientPalette.fats, this.nutrientFillAlpha),
        carbs: this.applyAlpha(this.nutrientPalette.carbs, this.nutrientFillAlpha),
        alcohol: this.applyAlpha(this.nutrientPalette.alcohol, this.nutrientFillAlpha),
    };
    public readonly nutrientTextColors = {
        calories: this.nutrientPalette.calories,
        fiber: this.nutrientPalette.fiber,
        proteins: this.nutrientPalette.proteins,
        fats: this.nutrientPalette.fats,
        carbs: this.nutrientPalette.carbs,
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
            proteinsPerBase: new FormControl(null, [Validators.required, Validators.min(0)]),
            fatsPerBase: new FormControl(null, [Validators.required, Validators.min(0)]),
            carbsPerBase: new FormControl(null, [Validators.required, Validators.min(0)]),
            fiberPerBase: new FormControl(null, [Validators.required, Validators.min(0)]),
            alcoholPerBase: new FormControl(0, [Validators.min(0)]),
            visibility: new FormControl(ProductVisibility.Private, { nonNullable: true, validators: Validators.required }),
        });

        this.buildUnitOptions();
        this.buildProductTypeOptions();
        this.buildVisibilityOptions();
        this.productForm.controls.baseUnit.valueChanges
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(unit => {
                if (!unit) {
                    return;
                }
                this.productForm.controls.baseAmount.setValue(this.getDefaultBaseAmount(unit));
            });
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.buildUnitOptions();
            this.buildProductTypeOptions();
            this.buildVisibilityOptions();
        });

        effect(() => {
            const currentProduct = this.product();
            if (currentProduct && !this.formInitialized) {
                this.populateForm(currentProduct);
                this.formInitialized = true;
            }
        });
    }

    public ngOnInit(): void {
        this.productForm.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.clearGlobalError();
            this.updateCalorieWarning();
        });

    }

    public stringifyUnits = (unit: MeasurementUnit): string => {
        return this.translateService.instant(`PRODUCT_MANAGE.DEFAULT_SERVING_UNITS.${MeasurementUnit[unit]}`);
    };

    public stringifyVisibility = (visibility: ProductVisibility): string => {
        return this.translateService.instant(`PRODUCT_MANAGE.VISIBILITY_OPTIONS.${visibility.toUpperCase()}`);
    };

    public readonly Unit = MeasurementUnit;
    public readonly Visibility = ProductVisibility;

    public get canShowDeleteButton(): boolean {
        const currentProduct = this.product();
        return !!currentProduct && currentProduct.isOwnedByCurrentUser;
    }

    public openBarcodeScanner(): void {
        this.fdDialogService
            .open<BarcodeScannerComponent, null, string | null>(BarcodeScannerComponent, { size: 'lg' })
            .afterClosed()
            .subscribe(barcode => {
                if (barcode) {
                    this.productForm.controls.barcode.setValue(barcode);
                }
            });
    }

    public async onCancel(): Promise<void> {
        await this.navigationService.navigateToProductList();
    }

    public async onDeleteProduct(): Promise<void> {
        const currentProduct = this.product();

        if (!currentProduct || !currentProduct.isOwnedByCurrentUser || this.isDeleting()) {
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

        const confirmed = await firstValueFrom(
            this.fdDialogService.open(ConfirmDeleteDialogComponent, { data, size: 'sm' }).afterClosed(),
        );

        if (!confirmed) {
            return;
        }

        this.isDeleting.set(true);
        this.clearGlobalError();

        try {
            await firstValueFrom(this.productService.deleteById(currentProduct.id));
            await this.navigationService.navigateToProductList();
        } catch (error) {
            this.isDeleting.set(false);
            this.setGlobalError('PRODUCT_MANAGE.DELETE_ERROR');
        }
    }

    public async onSubmit(): Promise<Product | null> {
        this.productForm.markAllAsTouched();

        if (this.productForm.valid) {
            const baseAmount = this.getNumberValue(this.productForm.controls.baseAmount);
            const defaultPortionAmount = this.getNumberValue(this.productForm.controls.defaultPortionAmount);
            const caloriesPerBase = this.getNumberValue(this.productForm.controls.caloriesPerBase);
            const proteinsPerBase = this.getNumberValue(this.productForm.controls.proteinsPerBase);
            const fatsPerBase = this.getNumberValue(this.productForm.controls.fatsPerBase);
            const carbsPerBase = this.getNumberValue(this.productForm.controls.carbsPerBase);
            const fiberPerBase = this.getNumberValue(this.productForm.controls.fiberPerBase);
            const alcoholPerBase = this.getNumberValue(this.productForm.controls.alcoholPerBase);

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

            return product
                ? await this.updateProduct(product.id, productData)
                : await this.addProduct(productData);
        }

        return null;
    }

    public get baseUnitLabel(): string {
        return this.getUnitLabel(this.productForm.controls.baseUnit.value);
    }

    public caloriesError(): string | null {
        const control = this.productForm.controls.caloriesPerBase;
        if (!control.touched && !control.dirty) {
            return null;
        }

        const calories = this.getNumberValue(control);
        return calories <= 0
            ? this.translateService.instant('PRODUCT_MANAGE.NUTRITION_ERRORS.CALORIES_REQUIRED')
            : null;
    }

    public macrosError(): string | null {
        const controls = [
            this.productForm.controls.proteinsPerBase,
            this.productForm.controls.fatsPerBase,
            this.productForm.controls.carbsPerBase,
        ];

        const shouldShow = controls.some(control => control.touched || control.dirty);
        if (!shouldShow) {
            return null;
        }

        const proteins = this.getNumberValue(this.productForm.controls.proteinsPerBase);
        const fats = this.getNumberValue(this.productForm.controls.fatsPerBase);
        const carbs = this.getNumberValue(this.productForm.controls.carbsPerBase);

        return proteins <= 0 && fats <= 0 && carbs <= 0
            ? this.translateService.instant('PRODUCT_MANAGE.NUTRITION_ERRORS.MACROS_REQUIRED')
            : null;
    }

    private updateCalorieWarning(): void {
        const calories = this.getNumberValue(this.productForm.controls.caloriesPerBase);
        const proteins = this.getNumberValue(this.productForm.controls.proteinsPerBase);
        const fats = this.getNumberValue(this.productForm.controls.fatsPerBase);
        const carbs = this.getNumberValue(this.productForm.controls.carbsPerBase);
        const alcohol = this.getNumberValue(this.productForm.controls.alcoholPerBase);
        const expectedCalories = this.nutritionCalculationService.calculateCaloriesFromMacros(proteins, fats, carbs, alcohol);

        if (expectedCalories <= 0 || calories <= 0) {
            this.nutritionWarning.set(null);
            return;
        }

        const deviation = Math.abs(calories - expectedCalories) / expectedCalories;
        if (deviation > this.calorieMismatchThreshold) {
            this.nutritionWarning.set({
                expectedCalories: Math.round(expectedCalories),
                actualCalories: Math.round(calories),
            });
            return;
        }

        this.nutritionWarning.set(null);
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

    private populateForm(product: Product): void {
        const normalizedVisibility = this.normalizeVisibility(product.visibility);
        const normalizedProductType =
            normalizeProductTypeValue(product.productType ?? product.category ?? null) ?? ProductType.Unknown;

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
                baseAmount: product.baseAmount,
                defaultPortionAmount: product.defaultPortionAmount,
                baseUnit: product.baseUnit,
                caloriesPerBase: product.caloriesPerBase,
                proteinsPerBase: product.proteinsPerBase,
                fatsPerBase: product.fatsPerBase,
                carbsPerBase: product.carbsPerBase,
                fiberPerBase: product.fiberPerBase,
                alcoholPerBase: product.alcoholPerBase,
                visibility: normalizedVisibility,
            },
            { emitEvent: false },
        );
        this.updateCalorieWarning();
    }

    private async addProduct(productData: CreateProductRequest): Promise<Product | null> {
        try {
            console.log('[ProductManage] add', productData);
            const product = await firstValueFrom(this.productService.create(productData));
            if (!this.skipConfirmDialog) {
                await this.showConfirmDialog();
            }
            return product;
        } catch (error) {
            this.handleSubmitError(error as HttpErrorResponse);
            return null;
        }
    }

    private async updateProduct(id: string, productData: Partial<CreateProductRequest>): Promise<Product | null> {
        try {
            console.log('[ProductManage] update', { id, data: productData });
            const product = await firstValueFrom(this.productService.update(id, productData));
            if (!this.skipConfirmDialog) {
                await this.showConfirmDialog();
            }
            return product;
        } catch (error) {
            this.handleSubmitError(error as HttpErrorResponse);
            return null;
        }
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

    private applyAlpha(hexColor: string, alpha: number): string {
        const normalized = hexColor.replace('#', '');
        const value = parseInt(normalized, 16);
        const r = (value >> 16) & 255;
        const g = (value >> 8) & 255;
        const b = value & 255;

        return `rgba(${r}, ${g}, ${b}, ${alpha})`;
    }

    private getNumberValue(control: FormControl<number | string | null>): number {
        const value = control.value as unknown;
        if (value === null || value === undefined || value === '') {
            return 0;
        }
        const parsed = Number(value);
        return Number.isFinite(parsed) ? parsed : 0;
    }

    private buildVisibilityOptions(): void {
        this.visibilitySelectOptions = this.visibilityOptions.map(option => ({
            value: option,
            label: this.translateService.instant(`PRODUCT_MANAGE.VISIBILITY_OPTIONS.${option.toUpperCase()}`),
        }));
    }

    private buildUnitOptions(): void {
        this.unitOptions = this.units.map(unit => ({
            value: unit,
            label: this.translateService.instant(`PRODUCT_AMOUNT_UNITS.${MeasurementUnit[unit]}`),
        }));
    }

    private buildProductTypeOptions(): void {
        this.productTypeSelectOptions = this.productTypes.map(type => ({
            value: type,
            label: this.translateService.instant(`PRODUCT_MANAGE.PRODUCT_TYPE_OPTIONS.${type.toUpperCase()}`),
        }));
    }

    private normalizeVisibility(value: ProductVisibility | null | string | undefined): ProductVisibility {
        if (typeof value !== 'string') {
            return ProductVisibility.Private;
        }

        const upper = value.toUpperCase();
        return upper === ProductVisibility.Public.toUpperCase() ? ProductVisibility.Public : ProductVisibility.Private;
    }

    private showConfirmDialog(): void {
        const data: ProductSaveSuccessDialogData = {
            isEdit: Boolean(this.product()),
        };

        this.fdDialogService
            .open<ProductSaveSuccessDialogComponent, ProductSaveSuccessDialogData, RedirectAction>(
                ProductSaveSuccessDialogComponent,
                { size: 'sm', data },
            )
            .afterClosed()
            .subscribe(redirectAction => {
                if (redirectAction === 'Home') {
                    this.navigationService.navigateToHome();
                } else if (redirectAction === 'ProductList') {
                    this.navigationService.navigateToProductList();
                }
            });
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

type ProductFormData = FormGroupControls<ProductFormValues>;

interface CalorieMismatchWarning {
    expectedCalories: number;
    actualCalories: number;
}

export type RedirectAction = 'Home' | 'ProductList';
