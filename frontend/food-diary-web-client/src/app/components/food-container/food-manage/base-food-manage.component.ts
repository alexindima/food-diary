import {
    ChangeDetectionStrategy,
    Component,
    DestroyRef,
    FactoryProvider,
    inject,
    input,
    OnInit,
    signal,
    TemplateRef,
    ViewChild,
} from '@angular/core';
import { Product, CreateProductRequest, MeasurementUnit, ProductVisibility } from '../../../types/product.data';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import {
    TuiButton,
    tuiDialog,
    TuiDialogContext,
    TuiDialogService,
    TuiError,
    TuiIcon,
    TuiLabel,
    TuiTextfieldComponent,
    TuiTextfieldDirective,
} from '@taiga-ui/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { TUI_VALIDATION_ERRORS, TuiFieldErrorPipe } from '@taiga-ui/kit';
import { AsyncPipe } from '@angular/common';
import { TuiInputNumberModule, TuiSelectModule, TuiTextfieldControllerModule } from '@taiga-ui/legacy';
import { ProductService } from '../../../services/product.service';
import { NavigationService } from '../../../services/navigation.service';
import { HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormGroupControls } from '../../../types/common.data';
import { NutrientChartData } from '../../../types/charts.data';
import {
    NutrientsSummaryComponent, NutrientsSummaryConfig
} from '../../shared/nutrients-summary/nutrients-summary.component';
import { CustomGroupComponent } from '../../shared/custom-group/custom-group.component';
import { firstValueFrom } from 'rxjs';
import { ZXingScannerModule } from '@zxing/ngx-scanner';
import { BarcodeScannerComponent } from '../../shared/barcode-scanner/barcode-scanner.component';
import { ValidationErrors } from '../../../types/validation-error.data';

export const VALIDATION_ERRORS_PROVIDER: FactoryProvider = {
    provide: TUI_VALIDATION_ERRORS,
    useFactory: (translate: TranslateService): ValidationErrors => ({
        required: () => translate.instant('FORM_ERRORS.REQUIRED'),
        min: ({ min }) =>
            translate.instant('FORM_ERRORS.INVALID_MIN_AMOUNT_MUST_BE_MORE_ZERO', {
                min,
            }),
    }),
    deps: [TranslateService],
};

@Component({
    selector: 'fd-base-food-manage',
    templateUrl: './base-food-manage.component.html',
    styleUrls: ['./base-food-manage.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [VALIDATION_ERRORS_PROVIDER],
    imports: [
        ReactiveFormsModule,
        TranslatePipe,
        TuiLabel,
        TuiError,
        TuiFieldErrorPipe,
        AsyncPipe,
        TuiButton,
        TuiSelectModule,
        TuiTextfieldControllerModule,
        TuiTextfieldComponent,
        TuiTextfieldDirective,
        TuiInputNumberModule,
        NutrientsSummaryComponent,
        CustomGroupComponent,
        ZXingScannerModule,
        TuiIcon,
    ]
})
export class BaseFoodManageComponent implements OnInit {
    protected readonly productService = inject(ProductService);
    protected readonly translateService = inject(TranslateService);
    protected readonly navigationService = inject(NavigationService);
    private readonly dialogService = inject(TuiDialogService);
    private readonly destroyRef = inject(DestroyRef);

    @ViewChild('confirmDialog') private confirmDialog!: TemplateRef<TuiDialogContext<RedirectAction, void>>;

    protected nutrientSummaryConfig: NutrientsSummaryConfig = {};

    public product = input<Product | null>();
    public globalError = signal<string | null>(null);
    public calories = signal<number>(0);
    public nutrientChartData = signal<NutrientChartData>({
        proteins: 0,
        fats: 0,
        carbs: 0,
    });

    private readonly barcodeDialog = tuiDialog(BarcodeScannerComponent, {
        dismissible: true,
        appearance: 'without-border-radius',
    });

    protected skipConfirmDialog = false;
    public productForm: FormGroup<ProductFormData>;
    public units = Object.values(MeasurementUnit) as MeasurementUnit[];
    public visibilityOptions = Object.values(ProductVisibility) as ProductVisibility[];
    public constructor() {
        this.productForm = new FormGroup<ProductFormData>({
            name: new FormControl('', { nonNullable: true, validators: Validators.required }),
            barcode: new FormControl(null),
            brand: new FormControl(null),
            category: new FormControl(null),
            description: new FormControl(null),
            imageUrl: new FormControl(null),
            baseAmount: new FormControl(100, { nonNullable: true, validators: [Validators.required, Validators.min(0.001)] }),
            baseUnit: new FormControl(MeasurementUnit.G, { nonNullable: true, validators: Validators.required }),
            caloriesPerBase: new FormControl(null, [Validators.required, Validators.min(0.001)]),
            proteinsPerBase: new FormControl(null, Validators.required),
            fatsPerBase: new FormControl(null, Validators.required),
            carbsPerBase: new FormControl(null, Validators.required),
            fiberPerBase: new FormControl(null, Validators.required),
            visibility: new FormControl(ProductVisibility.Private, { nonNullable: true, validators: Validators.required }),
        });
    }

    public ngOnInit(): void {
        const product = this.product();
        if (product) {
            this.populateForm(product);
            this.updateSummary();
        }

        this.productForm.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.clearGlobalError();
            this.updateSummary();
        });

    }

    public stringifyUnits = (unit: MeasurementUnit): string => {
        return this.translateService.instant(`FOOD_MANAGE.DEFAULT_SERVING_UNITS.${MeasurementUnit[unit]}`);
    };

    public stringifyVisibility = (visibility: ProductVisibility): string => {
        return this.translateService.instant(`FOOD_MANAGE.VISIBILITY_OPTIONS.${visibility.toUpperCase()}`);
    };

    public readonly Unit = MeasurementUnit;
    public readonly Visibility = ProductVisibility;

    public openBarcodeScanner(): void {
        this.barcodeDialog(null).subscribe({
            next: (barcode) => {
                this.productForm.controls.barcode.setValue(barcode);
            },
        });
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
                category: this.productForm.value.category || null,
                description: this.productForm.value.description || null,
                imageUrl: this.productForm.value.imageUrl || null,
                baseAmount: this.productForm.value.baseAmount!,
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

        const unitLabel = this.translateService.instant(`FOOD_AMOUNT_UNITS_SHORT.${baseUnit}`);
        return `${baseAmount} ${unitLabel}`;
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
        this.productForm.patchValue(product);
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

    private async showConfirmDialog(): Promise<void> {
        this.dialogService
            .open(this.confirmDialog, {
                dismissible: true,
                appearance: 'without-border-radius',
            })
            .subscribe(redirectAction => {
                if (redirectAction === 'Home') {
                    this.navigationService.navigateToHome();
                } else if (redirectAction === 'FoodList') {
                    this.navigationService.navigateToFoodList();
                }
            });
    }

    protected readonly MeasurementUnit = MeasurementUnit;
}

export interface ProductFormValues {
    name: string;
    barcode: string | null;
    brand: string | null;
    category: string | null;
    description: string | null;
    imageUrl: string | null;
    baseAmount: number;
    baseUnit: MeasurementUnit;
    caloriesPerBase: number | null;
    proteinsPerBase: number | null;
    fatsPerBase: number | null;
    carbsPerBase: number | null;
    fiberPerBase: number | null;
    visibility: ProductVisibility;
}

type ProductFormData = FormGroupControls<ProductFormValues>;

type RedirectAction = 'Home' | 'FoodList';
