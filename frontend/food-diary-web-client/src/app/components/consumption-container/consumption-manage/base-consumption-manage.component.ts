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
import { NavigationService } from '../../../services/navigation.service';
import {
    ProductListDialogComponent
} from '../../product-container/product-list/product-list-dialog/product-list-dialog.component';
import { RecipeSelectDialogComponent } from '../../recipe-container/recipe-select-dialog/recipe-select-dialog.component';
import { TuiDay, TuiTime } from '@taiga-ui/cdk';
import {
    Consumption,
    ConsumptionItemManageDto,
    ConsumptionManageDto,
    ConsumptionSourceType,
} from '../../../types/consumption.data';
import { ConsumptionService } from '../../../services/consumption.service';
import { FormGroupControls } from '../../../types/common.data';
import { Product } from '../../../types/product.data';
import { Recipe } from '../../../types/recipe.data';
import { HttpErrorResponse } from '@angular/common/http';
import { TuiUtils } from '../../../utils/tui.utils';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { nonEmptyArrayValidator } from '../../../validators/non-empty-array.validator';
import { NutrientChartData } from '../../../types/charts.data';
import {
    NutrientsSummaryComponent
} from '../../shared/nutrients-summary/nutrients-summary.component';
import { CustomGroupComponent } from '../../shared/custom-group/custom-group.component';
import { ValidationErrors } from '../../../types/validation-error.data';

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
    selector: 'fd-base-consumption-manage',
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
        CustomGroupComponent,
    ]
})
export class BaseConsumptionManageComponent implements OnInit {
    private readonly consumptionService = inject(ConsumptionService);
    private readonly translateService = inject(TranslateService);
    private readonly navigationService = inject(NavigationService);
    private readonly dialogService = inject(TuiDialogService);
    private readonly destroyRef = inject(DestroyRef);

    private readonly productListDialog = tuiDialog(ProductListDialogComponent, {
        size: 'page',
        dismissible: true,
        appearance: 'without-border-radius',
    });

    private readonly recipeSelectDialog = tuiDialog(RecipeSelectDialogComponent, {
        size: 'page',
        dismissible: true,
        appearance: 'without-border-radius',
    });

    @ViewChild('confirmDialog') private confirmDialog!: TemplateRef<TuiDialogContext<RedirectAction, void>>;

    public consumption = input<Consumption | null>();
    public totalCalories = signal<number>(0);
    public totalFiber = signal<number>(0);
    public nutrientChartData = signal<NutrientChartData>({
        proteins: 0,
        fats: 0,
        carbs: 0,
    });
    public globalError = signal<string | null>(null);

    public consumptionForm: FormGroup<ConsumptionFormData>;
    public sourceTypeOptions = Object.values(ConsumptionSourceType);

    public constructor() {
        this.consumptionForm = new FormGroup<ConsumptionFormData>({
            date: new FormControl<[TuiDay, TuiTime]>(
                [TuiDay.currentLocal(), TuiTime.currentLocal()], { nonNullable: true }
            ),
            items: new FormArray<FormGroup<ConsumptionItemFormData>>(
                [this.createConsumptionItem()],
                nonEmptyArrayValidator()
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

    public get items(): FormArray<FormGroup<ConsumptionItemFormData>> {
        return this.consumptionForm.controls.items;
    }

    public stringifySourceType = (value: ConsumptionSourceType | null): string =>
        value ? this.translateService.instant(`CONSUMPTION_MANAGE.ITEM_TYPE_OPTIONS.${value}`) : '';

    public isProductItem(index: number): boolean {
        return this.items.at(index).controls.sourceType.value === ConsumptionSourceType.Product;
    }

    public isRecipeItem(index: number): boolean {
        return this.items.at(index).controls.sourceType.value === ConsumptionSourceType.Recipe;
    }

    public getProductName(index: number): string {
        const control = this.items.at(index).controls.product;
        return control.value?.name || '';
    }

    public getRecipeName(index: number): string {
        const control = this.items.at(index).controls.recipe;
        return control.value?.name || '';
    }

    public getAmountUnitLabel(index: number): string | null {
        if (this.isProductItem(index)) {
            const unit = this.items.at(index).controls.product.value?.baseUnit;
            return unit ? this.translateService.instant('PRODUCT_AMOUNT_UNITS.' + unit.toUpperCase()) : null;
        }

        if (this.isRecipeItem(index)) {
            return this.translateService.instant('CONSUMPTION_MANAGE.SERVINGS_UNIT');
        }

        return null;
    }

    public isProductInvalid(index: number): boolean {
        if (!this.isProductItem(index)) {
            return false;
        }
        const control = this.items.at(index).controls.product;
        return control.invalid && control.touched;
    }

    public isRecipeInvalid(index: number): boolean {
        if (!this.isRecipeItem(index)) {
            return false;
        }
        const control = this.items.at(index).controls.recipe;
        return control.invalid && control.touched;
    }

    public addProductItem(): void {
        this.addItem(ConsumptionSourceType.Product);
    }

    public addRecipeItem(): void {
        this.addItem(ConsumptionSourceType.Recipe);
    }

    public addItem(type: ConsumptionSourceType): void {
        this.items.push(this.createConsumptionItem(null, null, null, type));
    }

    public removeItem(index: number): void {
        this.items.removeAt(index);
    }

    public onItemTypeChange(index: number, event: unknown): void {
        const type = event as ConsumptionSourceType | null;
        if (!type) {
            return;
        }
        const group = this.items.at(index);
        this.configureItemType(group, type, true);
    }

    public onProductSelectClick(index: number): void {
        this.productListDialog(null).subscribe({
            next: food => {
                if (!food) {
                    return;
                }
                const group = this.items.at(index);
                group.patchValue({
                    product: food,
                    recipe: null,
                });
                this.configureItemType(group, ConsumptionSourceType.Product);
            },
        });
    }

    public onRecipeSelectClick(index: number): void {
        this.recipeSelectDialog(null).subscribe({
            next: recipe => {
                if (!recipe) {
                    return;
                }
                const group = this.items.at(index);
                group.patchValue({
                    recipe,
                    product: null,
                });
                this.configureItemType(group, ConsumptionSourceType.Recipe);
            },
        });
    }

    public onSubmit(): void {
        this.markFormGroupTouched(this.consumptionForm);

        if (this.consumptionForm.invalid) {
            this.setGlobalError('FORM_ERRORS.UNKNOWN');
            return;
        }

        const tuiDateTime = this.consumptionForm.controls.date.value;
        const comment = this.consumptionForm.controls.comment.value;
        const formItems = this.consumptionForm.controls.items.value;

        const mappedItems: ConsumptionItemManageDto[] = [];

        formItems.forEach(item => {
            if (item.sourceType === ConsumptionSourceType.Product && item.product) {
                mappedItems.push({
                    productId: item.product.id,
                    recipeId: null,
                    amount: Number(item.amount),
                });
                return;
            }

            if (item.sourceType === ConsumptionSourceType.Recipe && item.recipe) {
                mappedItems.push({
                    recipeId: item.recipe.id,
                    productId: null,
                    amount: Number(item.amount),
                });
            }
        });

        const consumptionData: ConsumptionManageDto = {
            date: TuiUtils.combineTuiDayAndTuiTime(tuiDateTime[0], tuiDateTime[1]),
            comment: comment ?? undefined,
            items: mappedItems,
        };

        const consumption = this.consumption();
        consumption
            ? this.updateConsumption(consumption.id, consumptionData)
            : this.addConsumption(consumptionData);
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

        formGroup.markAllAsTouched();
    }

    private populateForm(consumption: Consumption): void {
        const date = new Date(consumption.date);
        const tuiDay = TuiDay.fromLocalNativeDate(date);
        const tuiTime = TuiTime.fromLocalNativeDate(date);

        this.consumptionForm.patchValue({
            date: [tuiDay, tuiTime],
            comment: consumption.comment || null,
        });

        const itemsArray = this.items;
        itemsArray.clear();

        if (consumption.items.length === 0) {
            itemsArray.push(this.createConsumptionItem());
            return;
        }

        consumption.items.forEach(item => {
            const sourceType = item.sourceType ?? (item.recipe ? ConsumptionSourceType.Recipe : ConsumptionSourceType.Product);
            itemsArray.push(this.createConsumptionItem(
                sourceType === ConsumptionSourceType.Product ? item.product ?? null : null,
                sourceType === ConsumptionSourceType.Recipe ? item.recipe ?? null : null,
                item.amount,
                sourceType,
            ));
        });
    }

    private updateSummary(): void {
        const totals = this.items.controls.reduce(
            (totals, group) => {
                const sourceType = group.controls.sourceType.value;
                const amount = group.controls.amount.value || 0;

                if (sourceType === ConsumptionSourceType.Product) {
                    const food = group.controls.product.value as Product | null;
                    if (!food || food.baseAmount <= 0) {
                        return totals;
                    }
                    const multiplier = amount / food.baseAmount;
                    totals.calories += food.caloriesPerBase * multiplier;
                    totals.proteins += food.proteinsPerBase * multiplier;
                    totals.fats += food.fatsPerBase * multiplier;
                    totals.carbs += food.carbsPerBase * multiplier;
                    totals.fiber += (food.fiberPerBase ?? 0) * multiplier;
                    return totals;
                }

                const recipe = group.controls.recipe.value as Recipe | null;
                if (recipe && recipe.servings && recipe.servings > 0) {
                    const servings = recipe.servings <= 0 ? 1 : recipe.servings;
                    const caloriesPerServing = (recipe.totalCalories ?? 0) / servings;
                    const proteinsPerServing = (recipe.totalProteins ?? 0) / servings;
                    const fatsPerServing = (recipe.totalFats ?? 0) / servings;
                    const carbsPerServing = (recipe.totalCarbs ?? 0) / servings;
                    const fiberPerServing = (recipe.totalFiber ?? 0) / servings;

                    totals.calories += caloriesPerServing * amount;
                    totals.proteins += proteinsPerServing * amount;
                    totals.fats += fatsPerServing * amount;
                    totals.carbs += carbsPerServing * amount;
                    totals.fiber += fiberPerServing * amount;
                }

                return totals;
            },
            { calories: 0, proteins: 0, fats: 0, carbs: 0, fiber: 0 }
        );

        if (this.totalCalories() !== totals.calories) {
            this.totalCalories.set(totals.calories);
        }

        if (this.totalFiber() !== totals.fiber) {
            this.totalFiber.set(totals.fiber);
        }

        const currentNutrientData = this.nutrientChartData();
        if (
            currentNutrientData.proteins !== totals.proteins ||
            currentNutrientData.fats !== totals.fats ||
            currentNutrientData.carbs !== totals.carbs
        ) {
            this.nutrientChartData.set({
                proteins: totals.proteins,
                fats: totals.fats,
                carbs: totals.carbs,
            });
        }
    }

    private async addConsumption(consumptionData: ConsumptionManageDto): Promise<void> {
        this.consumptionService.create(consumptionData).subscribe({
            next: response => this.handleSubmitResponse(response),
            error: error => this.handleSubmitError(error),
        });
    }

    private async updateConsumption(id: number, consumptionData: ConsumptionManageDto): Promise<void> {
        this.consumptionService.update(id, consumptionData).subscribe({
            next: response => this.handleSubmitResponse(response),
            error: error => this.handleSubmitError(error),
        });
    }

    private async handleSubmitResponse(response: Consumption | null): Promise<void> {
        if (response) {
            if (!this.consumption()) {
                this.consumptionForm.reset();
                this.items.clear();
                this.items.push(this.createConsumptionItem());
            }
            await this.showConfirmDialog();
        } else {
            this.handleSubmitError();
        }
    }

    private handleSubmitError(error?: HttpErrorResponse): void {
        this.setGlobalError('FORM_ERRORS.UNKNOWN');
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

    private configureItemType(
        group: FormGroup<ConsumptionItemFormData>,
        type: ConsumptionSourceType,
        clearSelection: boolean = false,
    ): void {
        group.controls.sourceType.setValue(type);

        if (type === ConsumptionSourceType.Product) {
            group.controls.product.setValidators([Validators.required]);
            group.controls.recipe.clearValidators();
            if (clearSelection) {
                group.controls.recipe.setValue(null);
            }
        } else {
            group.controls.recipe.setValidators([Validators.required]);
            group.controls.product.clearValidators();
            if (clearSelection) {
                group.controls.product.setValue(null);
            }
        }

        group.controls.product.updateValueAndValidity();
        group.controls.recipe.updateValueAndValidity();

        if (clearSelection) {
            group.controls.amount.setValue(null);
            group.controls.amount.markAsUntouched();
        }
    }

    private createConsumptionItem(
        product: Product | null = null,
        recipe: Recipe | null = null,
        amount: number | null = null,
        sourceType: ConsumptionSourceType = ConsumptionSourceType.Product,
    ): FormGroup<ConsumptionItemFormData> {
        const group = new FormGroup<ConsumptionItemFormData>({
        sourceType: new FormControl<ConsumptionSourceType>(sourceType, { nonNullable: true }),
        product: new FormControl<Product | null>(product),
        recipe: new FormControl<Recipe | null>(recipe),
        amount: new FormControl<number | null>(amount, [Validators.required, Validators.min(0.01)]),
        });

        this.configureItemType(group, sourceType);
        return group;
    }
}

type RedirectAction = 'Home' | 'ConsumptionList';

type ConsumptionFormValues = {
    date: [TuiDay, TuiTime];
    items: ConsumptionItemFormValues[];
    comment: string | null;
};

type ConsumptionItemFormValues = {
    sourceType: ConsumptionSourceType;
    product: Product | null;
    recipe: Recipe | null;
    amount: number | null;
};

type ConsumptionFormData = FormGroupControls<ConsumptionFormValues>;

type ConsumptionItemFormData = FormGroupControls<ConsumptionItemFormValues>;



