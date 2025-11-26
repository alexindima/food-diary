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
import { DecimalPipe } from '@angular/common';
import { ProductService } from '../../../services/product.service';
import { NavigationService } from '../../../services/navigation.service';
import { HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormGroupControls } from '../../../types/common.data';
import { NutrientData } from '../../../types/charts.data';
import {
    NutrientsSummaryComponent, NutrientsSummaryConfig
} from '../../shared/nutrients-summary/nutrients-summary.component';
import { firstValueFrom } from 'rxjs';
import { ZXingScannerModule } from '@zxing/ngx-scanner';
import { BarcodeScannerComponent } from '../../shared/barcode-scanner/barcode-scanner.component';
import { FdUiFormErrorComponent, FD_VALIDATION_ERRORS, FdValidationErrors } from 'fd-ui-kit/form-error/fd-ui-form-error.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiSelectComponent, FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select.component';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea.component';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { normalizeProductType as normalizeProductTypeValue } from '../../../utils/product-type.utils';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import {
    ProductSaveSuccessDialogComponent,
    ProductSaveSuccessDialogData,
} from './product-save-success-dialog.component';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { FdPageContainerDirective } from '../../../directives/layout/page-container.directive';

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
        DecimalPipe,
        NutrientsSummaryComponent,
        ZXingScannerModule,
        FdUiInputComponent,
        FdUiCardComponent,
        FdUiSelectComponent,
        FdUiTextareaComponent,
        FdUiButtonComponent,
        FdUiFormErrorComponent,
        PageHeaderComponent,
        FdPageContainerDirective,
    ],
})
export class BaseProductManageComponent implements OnInit {
    protected readonly productService = inject(ProductService);
    protected readonly translateService = inject(TranslateService);
    protected readonly navigationService = inject(NavigationService);
    protected readonly fdDialogService = inject(FdUiDialogService);
    private readonly destroyRef = inject(DestroyRef);

    protected nutrientSummaryConfig: NutrientsSummaryConfig = {};

    public product = input<Product | null>();
    public globalError = signal<string | null>(null);
    public calories = signal<number>(0);
    public nutrientChartData = signal<NutrientData>({
        proteins: 0,
        fats: 0,
        carbs: 0,
    });
    public readonly isDeleting = signal(false);

    protected skipConfirmDialog = false;
    public productForm: FormGroup<ProductFormData>;
    public units = Object.values(MeasurementUnit) as MeasurementUnit[];
    public unitOptions: FdUiSelectOption<MeasurementUnit>[] = [];
    public productTypes = Object.values(ProductType) as ProductType[];
    public productTypeSelectOptions: FdUiSelectOption<ProductType>[] = [];
    public visibilityOptions = Object.values(ProductVisibility) as ProductVisibility[];
    public visibilitySelectOptions: FdUiSelectOption<ProductVisibility>[] = [];
    public constructor() {
        this.productForm = new FormGroup<ProductFormData>({
            name: new FormControl('', { nonNullable: true, validators: Validators.required }),
            barcode: new FormControl(null),
            brand: new FormControl(null),
            productType: new FormControl<ProductType>(ProductType.Unknown, { nonNullable: true }),
            description: new FormControl(null),
            comment: new FormControl(null),
            imageUrl: new FormControl(null),
            baseAmount: new FormControl(100, { nonNullable: true, validators: [Validators.required, Validators.min(0.001)] }),
            defaultPortionAmount: new FormControl(100, {
                nonNullable: true,
                validators: [Validators.required, Validators.min(0.001)],
            }),
            baseUnit: new FormControl(MeasurementUnit.G, { nonNullable: true, validators: Validators.required }),
            caloriesPerBase: new FormControl(null, [Validators.required, Validators.min(0.001)]),
            proteinsPerBase: new FormControl(null, Validators.required),
            fatsPerBase: new FormControl(null, Validators.required),
            carbsPerBase: new FormControl(null, Validators.required),
            fiberPerBase: new FormControl(null, Validators.required),
            visibility: new FormControl(ProductVisibility.Private, { nonNullable: true, validators: Validators.required }),
        });

        this.buildUnitOptions();
        this.buildProductTypeOptions();
        this.buildVisibilityOptions();
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.buildUnitOptions();
            this.buildProductTypeOptions();
            this.buildVisibilityOptions();
        });

        effect(() => {
            const currentProduct = this.product();
            if (currentProduct) {
                this.populateForm(currentProduct);
                this.updateSummary();
            }
        });
    }

    public ngOnInit(): void {
        this.productForm.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.clearGlobalError();
            this.updateSummary();
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

        const confirmationMessage = this.translateService.instant('PRODUCT_MANAGE.DELETE_CONFIRM');

        if (!window.confirm(confirmationMessage)) {
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

        if (!this.isMacronutrientsValid()) {
            this.setGlobalError('FORM_ERRORS.AT_LEAST_ONE_MACRONUTRIENT_MUST_BE_SET');
            return null;
        }

        if (this.productForm.valid) {
            const productData: CreateProductRequest = {
                name: this.productForm.value.name!,
                barcode: this.productForm.value.barcode || null,
                brand: this.productForm.value.brand || null,
                productType: this.productForm.value.productType || ProductType.Unknown,
                category: this.productForm.value.productType || null,
                description: this.productForm.value.description || null,
                comment: this.productForm.value.comment || null,
                imageUrl: this.productForm.value.imageUrl || null,
                baseAmount: this.productForm.value.baseAmount!,
                defaultPortionAmount: this.productForm.value.defaultPortionAmount!,
                baseUnit: this.productForm.value.baseUnit!,
                caloriesPerBase: this.productForm.value.caloriesPerBase!,
                proteinsPerBase: this.productForm.value.proteinsPerBase!,
                fatsPerBase: this.productForm.value.fatsPerBase!,
                carbsPerBase: this.productForm.value.carbsPerBase!,
                fiberPerBase: this.productForm.value.fiberPerBase!,
                visibility: this.productForm.value.visibility!,
            };
            const product = this.product();

            return product
                ? await this.updateProduct(product.id, productData)
                : await this.addProduct(productData);
        }

        return null;
    }

    public get getDynamicNutrientPlaceholder(): string {
        const baseAmount = this.productForm.controls.baseAmount.value ?? 0;
        const baseUnit = this.productForm.controls.baseUnit.value;

        const unitLabel = this.translateService.instant(`PRODUCT_AMOUNT_UNITS_SHORT.${baseUnit}`);
        return `${baseAmount} ${unitLabel}`;
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

    private updateSummary(): void {
        const caloriesPerBase = this.productForm.controls.caloriesPerBase.value ?? 0;
        const proteinsPerBase = this.productForm.controls.proteinsPerBase.value ?? 0;
        const fatsPerBase = this.productForm.controls.fatsPerBase.value ?? 0;
        const carbsPerBase = this.productForm.controls.carbsPerBase.value ?? 0;

        const newTotalCalories = caloriesPerBase;
        const newNutrientChartData = {
            proteins: proteinsPerBase,
            fats: fatsPerBase,
            carbs: carbsPerBase,
        };

        if (this.calories() !== newTotalCalories) {
            this.calories.set(newTotalCalories);
        }

        if (
            this.nutrientChartData().proteins !== newNutrientChartData.proteins ||
            this.nutrientChartData().fats !== newNutrientChartData.fats ||
            this.nutrientChartData().carbs !== newNutrientChartData.carbs
        ) {
            this.nutrientChartData.set(newNutrientChartData);
        }
    }

    private isMacronutrientsValid(): boolean {
        const { proteinsPerBase, fatsPerBase, carbsPerBase } = this.productForm.value;
        return (proteinsPerBase ?? 0) + (fatsPerBase ?? 0) + (carbsPerBase ?? 0) > 0;
    }

    private populateForm(product: Product): void {
        const normalizedVisibility = this.normalizeVisibility(product.visibility);
        const normalizedProductType =
            normalizeProductTypeValue(product.productType ?? product.category ?? null) ?? ProductType.Unknown;

        this.productForm.patchValue({
            name: product.name,
            barcode: product.barcode ?? null,
            brand: product.brand ?? null,
            productType: normalizedProductType,
            description: product.description ?? null,
            comment: product.comment ?? null,
            imageUrl: product.imageUrl ?? null,
            baseAmount: product.baseAmount,
            defaultPortionAmount: product.defaultPortionAmount,
            baseUnit: product.baseUnit,
            caloriesPerBase: product.caloriesPerBase,
            proteinsPerBase: product.proteinsPerBase,
            fatsPerBase: product.fatsPerBase,
            carbsPerBase: product.carbsPerBase,
            fiberPerBase: product.fiberPerBase,
            visibility: normalizedVisibility,
        });
    }

    private async addProduct(productData: CreateProductRequest): Promise<Product | null> {
        try {
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
    imageUrl: string | null;
    baseAmount: number;
    defaultPortionAmount: number;
    baseUnit: MeasurementUnit;
    caloriesPerBase: number | null;
    proteinsPerBase: number | null;
    fatsPerBase: number | null;
    carbsPerBase: number | null;
    fiberPerBase: number | null;
    visibility: ProductVisibility;
}

type ProductFormData = FormGroupControls<ProductFormValues>;

export type RedirectAction = 'Home' | 'ProductList';
