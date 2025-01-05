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
    ViewChild
} from '@angular/core';
import { FormArray, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import {
    TuiButton,
    tuiDialog,
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
import {
    TuiInputDateTimeModule,
    TuiInputNumberModule,
    TuiMultiSelectModule,
    TuiSelectModule,
    TuiTextfieldControllerModule,
} from '@taiga-ui/legacy';
import { ApiResponse, ErrorCode } from '../../../types/api-response.data';
import { NavigationService } from '../../../services/navigation.service';
import {
    FoodListDialogComponent
} from '../../food-container/food-list/food-list-dialog/food-list-dialog.component';
import { TuiDay, TuiTime } from '@taiga-ui/cdk';
import {
    Consumption,
    ConsumptionManageDto,
} from '../../../types/consumption.data';
import { ConsumptionService } from '../../../services/consumption.service';
import { FormGroupControls } from '../../../types/common.data';
import { Food } from '../../../types/food.data';
import { TuiUtils } from '../../../utils/tui.utils';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { nonEmptyArrayValidator } from '../../../validators/non-empty-array.validator';
import { NutrientChartData } from '../../../types/charts.data';
import {
    NutrientsSummaryComponent
} from '../../shared/nutrients-summary/nutrients-summary.component';

export const VALIDATION_ERRORS_PROVIDER: FactoryProvider = {
    provide: TUI_VALIDATION_ERRORS,
    useFactory: (translate: TranslateService): ValidationErrors => ({
        required: () => translate.instant('FORM_ERRORS.REQUIRED'),
        nonEmptyArray: () => translate.instant('FORM_ERRORS.NON_EMPTY_ARRAY'),
        min: ({ min }) =>
            translate.instant('FORM_ERRORS.INVALID_MIN_AMOUNT_MUST_BE_MORE_ZERO', {
                min,
            }),
    }),
    deps: [TranslateService],
};

@Component({
    selector: 'app-base-consumption-manage',
    templateUrl: './base-consumption-manage.component.html',
    styleUrls: ['./base-consumption-manage.component.less'],
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
        TuiMultiSelectModule,
        TuiInputDateTimeModule,
        NutrientsSummaryComponent,
    ]
})
export class BaseConsumptionManageComponent implements OnInit {
    private readonly consumptionService = inject(ConsumptionService);
    private readonly translateService = inject(TranslateService);
    private readonly navigationService = inject(NavigationService);
    private readonly dialogService = inject(TuiDialogService);
    private readonly destroyRef = inject(DestroyRef);

    private readonly dialog = tuiDialog(FoodListDialogComponent, {
        size: 'page',
        dismissible: true,
        appearance: 'without-border-radius',
    });

    @ViewChild('confirmDialog') private confirmDialog!: TemplateRef<TuiDialogContext<RedirectAction, void>>;

    public consumption = input<Consumption | null>();
    public totalCalories = signal<number>(0);
    public nutrientChartData = signal<NutrientChartData>({
        proteins: 0,
        fats: 0,
        carbs: 0,
    });
    public globalError = signal<string | null>(null);

    public consumptionForm: FormGroup<ConsumptionFormData>;
    public selectedIndex: number = 0;

    public constructor() {
        this.consumptionForm = new FormGroup<ConsumptionFormData>({
            date: new FormControl<[TuiDay, TuiTime]>(
                [TuiDay.currentLocal(), TuiTime.currentLocal()], { nonNullable: true }
            ),
            consumedFood: new FormArray<FormGroup<ConsumptionItemFormData>>(
                [this.createConsumedFoodItem()], nonEmptyArrayValidator()
            ),
            comment: new FormControl<string | null>(null),
        });
    }

    public ngOnInit(): void {
        const consumption = this.consumption();
        if (consumption) {
            this.populateForm(consumption);
            this.updateSummary();
        }

        this.consumptionForm.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.updateSummary();
            this.clearGlobalError();
        });
    }

    public get consumedFood(): FormArray<FormGroup<ConsumptionItemFormData>> {
        return this.consumptionForm.controls.consumedFood;
    }

    public getFoodName(index: number): string {
        const foodControl = this.consumedFood.at(index).controls.food;
        return foodControl?.value?.name || '';
    }

    public getFoodUnit(index: number): string | null {
        const unit = this.consumedFood.at(index).controls.food?.value?.baseUnit;
        return unit
            ? `, ${this.translateService.instant('FOOD_AMOUNT_UNITS.' + unit.toUpperCase())}`
            : null;
    }

    public isFoodInvalid(index: number): boolean {
        const foodControl = this.consumedFood.at(index).controls.food;
        return !!foodControl && foodControl.invalid && foodControl.touched;
    }

    public addFoodItem(): void {
        this.consumedFood.push(this.createConsumedFoodItem());
    }

    public removeFoodItem(index: number): void {
        this.consumedFood.removeAt(index);
    }

    public async onFoodSelectClick(index: number): Promise<void> {
        this.selectedIndex = index;
        this.dialog(null).subscribe({
            next: food => {
                const consumedFoodGroup = this.consumedFood.at(this.selectedIndex);
                consumedFoodGroup.patchValue({ food });
            },
        });
    }

    public onSubmit(): void {
        this.markFormGroupTouched(this.consumptionForm);

        if (this.consumptionForm.valid) {
            const tuiDateTime = this.consumptionForm.controls.date.value;
            const comment = this.consumptionForm.controls.comment.value;
            const consumedFood = this.consumptionForm.controls.consumedFood.value;

            const consumptionData: ConsumptionManageDto = {
                date: TuiUtils.combineTuiDayAndTuiTime(tuiDateTime[0], tuiDateTime[1]),
                comment: comment ?? undefined,
                items: consumedFood
                    .filter(foodItem => foodItem.food && foodItem.quantity !== null)
                    .map(foodItem => ({
                        foodId: foodItem.food!.id,
                        amount: Number(foodItem.quantity),
                    })),
            };

            const consumption = this.consumption();
            consumption
                ? this.updateConsumption(consumption.id, consumptionData)
                : this.addConsumption(consumptionData);
        }
    }

    private markFormGroupTouched(formGroup: FormGroup | FormArray): void {
        Object.values(formGroup.controls).forEach(control => {
            if (control instanceof FormGroup || control instanceof FormArray) {
                this.markFormGroupTouched(control);
            } else {
                control.markAllAsTouched();
                control.updateValueAndValidity();
            }
        });
    }

    private populateForm(consumption: Consumption): void {
        const date = new Date(consumption.date);
        const tuiDay = TuiDay.fromLocalNativeDate(date);
        const tuiTime = TuiTime.fromLocalNativeDate(date);

        this.consumptionForm.patchValue({
            date: [tuiDay, tuiTime],
            comment: consumption.comment || null,
        });

        const consumedFoodArray = this.consumedFood;
        consumedFoodArray.clear();

        consumption.items.forEach((item) => {
            consumedFoodArray.push(this.createConsumedFoodItem(item.food, item.amount));
        });
    }

    private updateSummary(): void {
        const totals = this.consumedFood.controls.reduce(
            (totals, group) => {
                const food = group.value.food as Food | null;
                const quantity = group.value.quantity || 0;

                if (food) {
                    const multiplier = quantity / food.baseAmount;
                    totals.calories += food.caloriesPerBase * multiplier;
                    totals.proteins += food.proteinsPerBase * multiplier;
                    totals.fats += food.fatsPerBase * multiplier;
                    totals.carbs += food.carbsPerBase * multiplier;
                }

                return totals;
            },
            { calories: 0, proteins: 0, fats: 0, carbs: 0 }
        );

        this.totalCalories.set(totals.calories)
        this.nutrientChartData.set({
            proteins: totals.proteins,
            fats: totals.fats,
            carbs: totals.carbs,
        })
    }

    private async addConsumption(consumptionData: ConsumptionManageDto): Promise<void> {
        this.consumptionService.create(consumptionData).subscribe({
            next: response => this.handleSubmitResponse(response),
        });
    }

    private async updateConsumption(id: number, consumptionData: ConsumptionManageDto): Promise<void> {
        const requestData = {
            date: consumptionData.date,
            comment: consumptionData.comment,
            items: consumptionData.items.map(item => ({
                foodId: item.foodId,
                amount: item.amount,
            })),
        };

        this.consumptionService.update(id, requestData).subscribe({
            next: response => this.handleSubmitResponse(response),
        });
    }

    private async handleSubmitResponse(response: ApiResponse<Consumption | null>): Promise<void> {
        if (response.status === 'success') {
            if (!this.consumption()) {
                this.consumptionForm.reset();
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
                } else if (redirectAction === 'ConsumptionList') {
                    this.navigationService.navigateToConsumptionList();
                }
            });
    }

    private createConsumedFoodItem(food: Food | null = null, quantity: number | null = null): FormGroup<ConsumptionItemFormData> {
        return new FormGroup<ConsumptionItemFormData>({
            food: new FormControl<Food | null>(food, Validators.required),
            quantity: new FormControl<number | null>(quantity, [Validators.required, Validators.min(0.01)]),
        });
    }
}

interface ValidationErrors {
    required: () => string;
    nonEmptyArray: () => string;
    min: (_params: { min: string }) => string;
}

type RedirectAction = 'Home' | 'ConsumptionList';

type ConsumptionFormValues = {
    date: [TuiDay, TuiTime];
    consumedFood: ConsumptionItemFormValues[];
    comment: string | null;
};

type ConsumptionItemFormValues ={
    food: Food | null;
    quantity: number | null;
};

type ConsumptionFormData = FormGroupControls<ConsumptionFormValues>;

type ConsumptionItemFormData = FormGroupControls<ConsumptionItemFormValues>;
