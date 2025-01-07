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
import { Food, FoodManageDto, Unit } from '../../../types/food.data';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import {
    TuiButton,
    TuiDialogContext,
    TuiDialogService,
    TuiError,
    TuiLabel,
    TuiTextfieldComponent,
    TuiTextfieldDirective,
} from '@taiga-ui/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { TUI_VALIDATION_ERRORS, TuiFieldErrorPipe } from '@taiga-ui/kit';
import { AsyncPipe } from '@angular/common';
import { TuiInputNumberModule, TuiSelectModule, TuiTextfieldControllerModule } from '@taiga-ui/legacy';
import { ApiResponse, ErrorCode } from '../../../types/api-response.data';
import { FoodService } from '../../../services/food.service';
import { NavigationService } from '../../../services/navigation.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormGroupControls } from '../../../types/common.data';
import { NutrientChartData } from '../../../types/charts.data';
import {
    NutrientsSummaryComponent
} from '../../shared/nutrients-summary/nutrients-summary.component';
import { CustomGroupComponent } from '../../shared/custom-group/custom-group.component';

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
    selector: 'app-base-food-manage',
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
    ]
})
export class BaseFoodManageComponent implements OnInit {
    protected readonly foodService = inject(FoodService);
    protected readonly translateService = inject(TranslateService);
    protected readonly navigationService = inject(NavigationService);
    private readonly dialogService = inject(TuiDialogService);
    private readonly destroyRef = inject(DestroyRef);

    @ViewChild('confirmDialog') private confirmDialog!: TemplateRef<TuiDialogContext<RedirectAction, void>>;

    public food = input<Food | null>();
    public globalError = signal<string | null>(null);
    public calories = signal<number>(0);
    public nutrientChartData = signal<NutrientChartData>({
        proteins: 0,
        fats: 0,
        carbs: 0,
    });

    public foodForm: FormGroup<FoodFormData>;
    public units = Object.values(Unit) as Unit[];

    public constructor() {
        this.foodForm = new FormGroup<FoodFormData>({
            name: new FormControl('', { nonNullable: true, validators: Validators.required }),
            barcode: new FormControl(null),
            category: new FormControl(null),
            baseAmount: new FormControl(100, { nonNullable: true, validators: [Validators.required, Validators.min(0.001)] }),
            baseUnit: new FormControl(Unit.G, { nonNullable: true, validators: Validators.required }),
            caloriesPerBase: new FormControl(null, [Validators.required, Validators.min(0.001)]),
            proteinsPerBase: new FormControl(null, Validators.required),
            fatsPerBase: new FormControl(null, Validators.required),
            carbsPerBase: new FormControl(null, Validators.required),
        });
    }

    public ngOnInit(): void {
        const food = this.food();
        if (food) {
            this.populateForm(food);
            this.updateSummary();
        }

        this.foodForm.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.clearGlobalError();
            this.updateSummary();
        });

    }

    public stringifyUnits = (unit: Unit): string => {
        return this.translateService.instant(`FOOD_MANAGE.DEFAULT_SERVING_UNITS.${Unit[unit]}`);
    };

    public onSubmit(): void {
        this.foodForm.markAllAsTouched();

        if (!this.isMacronutrientsValid()) {
            this.setGlobalError('FORM_ERRORS.AT_LEAST_ONE_MACRONUTRIENT_MUST_BE_SET');
            return;
        }

        if (this.foodForm.valid) {
            const foodData = new FoodManageDto(this.foodForm.value);
            const food = this.food();
            food ? this.updateFood(food.id, foodData) : this.addFood(foodData);
        }
    }

    public get getDynamicNutrientPlaceholder(): string {
        const baseAmount = this.foodForm.controls.baseAmount.value ?? 0;
        const baseUnit = this.foodForm.controls.baseUnit.value;

        const unitLabel = this.translateService.instant(`FOOD_AMOUNT_UNITS_SHORT.${baseUnit}`);
        return `${baseAmount} ${unitLabel}`;
    }

    private updateSummary(): void {
        const caloriesPerBase = this.foodForm.controls.caloriesPerBase.value ?? 0;
        const proteinsPerBase = this.foodForm.controls.proteinsPerBase.value ?? 0;
        const fatsPerBase = this.foodForm.controls.fatsPerBase.value ?? 0;
        const carbsPerBase = this.foodForm.controls.carbsPerBase.value ?? 0;

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
        const { proteinsPerBase, fatsPerBase, carbsPerBase } = this.foodForm.value;
        return (proteinsPerBase ?? 0) + (fatsPerBase ?? 0) + (carbsPerBase ?? 0) > 0;
    }

    private populateForm(food: Food): void {
        this.foodForm.patchValue(food);
    }

    private async addFood(foodData: FoodManageDto): Promise<void> {
        this.foodService.create(foodData).subscribe({
            next: response => this.handleSubmitResponse(response),
        });
    }

    private async updateFood(id: number, foodData: FoodManageDto): Promise<void> {
        this.foodService.update(id, foodData).subscribe({
            next: response => this.handleSubmitResponse(response),
        });
    }

    private async handleSubmitResponse(response: ApiResponse<Food | null>): Promise<void> {
        if (response.status === 'success') {
            if (!this.food()) {
                this.foodForm.reset();
            }
            await this.showConfirmDialog();
        } else if (response.status === 'error') {
            this.handleSubmitError(response.error);
        }
    }

    private handleSubmitError(error?: ErrorCode): void {
        if (error === ErrorCode.INVALID_CREDENTIALS) {
            this.setGlobalError('FORM_ERRORS.INVALID_CREDENTIALS');
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

    protected readonly Unit = Unit;
}

export interface FoodFormValues {
    name: string;
    barcode: string | null;
    category: string | null;
    baseAmount: number;
    baseUnit: Unit;
    caloriesPerBase: number | null;
    proteinsPerBase: number | null;
    fatsPerBase: number | null;
    carbsPerBase: number | null;
}

type FoodFormData = FormGroupControls<FoodFormValues>;

interface ValidationErrors {
    required: () => string;
    min: (_params: { min: string }) => string;
}

type RedirectAction = 'Home' | 'FoodList';
