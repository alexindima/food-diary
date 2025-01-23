import {
    ChangeDetectionStrategy,
    Component,
    FactoryProvider,
    inject,
    input,
    OnInit,
    signal
} from '@angular/core';
import { FormArray, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { FormGroupControls } from '../../../types/common.data';
import {
    TuiButton, tuiDialog,
    TuiError,
    TuiLabel,
    TuiTextfieldComponent,
    TuiTextfieldDirective
} from '@taiga-ui/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { TuiInputNumberModule, TuiSelectModule, TuiTextareaModule } from '@taiga-ui/legacy';
import { CustomGroupComponent } from '../../shared/custom-group/custom-group.component';
import { Food } from '../../../types/food.data';
import { AsyncPipe, NgForOf } from '@angular/common';
import { TUI_VALIDATION_ERRORS, TuiFieldErrorPipe } from '@taiga-ui/kit';
import { nonEmptyArrayValidator } from '../../../validators/non-empty-array.validator';
import {
    FoodListDialogComponent
} from '../../food-container/food-list/food-list-dialog/food-list-dialog.component';
import { NutrientChartData } from '../../../types/charts.data';
import { ApiResponse, ErrorCode } from '../../../types/api-response.data';
import {
    NutrientsSummaryComponent
} from '../../shared/nutrients-summary/nutrients-summary.component';
import { ValidationErrors } from '../../../types/validation-error.data';
import { Recipe, RecipeDto } from '../../../types/recipe.data';
import { RecipeService } from "../../../services/recipe.service";
import { Consumption } from "../../../types/consumption.data";
import { DropZoneDirective } from "../../../directives/drop-zone.directive";
import { DraggableDirective } from "../../../directives/draggable.directive";

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
    selector: 'fd-recipe-manage',
    imports: [
        ReactiveFormsModule,
        TuiTextfieldComponent,
        TranslatePipe,
        TuiLabel,
        TuiTextfieldDirective,
        TuiTextareaModule,
        TuiSelectModule,
        TuiInputNumberModule,
        CustomGroupComponent,
        TuiButton,
        NgForOf,
        AsyncPipe,
        TuiError,
        TuiFieldErrorPipe,
        NutrientsSummaryComponent,
        DropZoneDirective,
        DraggableDirective,
    ],
    templateUrl: './recipe-manage.component.html',
    styleUrl: './recipe-manage.component.less',
    providers: [VALIDATION_ERRORS_PROVIDER],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class RecipeManageComponent implements OnInit {
    private readonly recipeService = inject(RecipeService);
    private readonly translateService = inject(TranslateService);

    public recipe = input<boolean>();
    public totalCalories = signal<number>(0);
    public nutrientChartData = signal<NutrientChartData>({
        proteins: 0,
        fats: 0,
        carbs: 0,
    });
    public globalError = signal<string | null>(null);

    public recipeForm: FormGroup<RecipeFormData>;
    public selectedStepIndex: number = 0;
    public selectedIngredientIndex: number = 0;

    private readonly foodListDialog = tuiDialog(FoodListDialogComponent, {
        size: 'page',
        dismissible: true,
        appearance: 'without-border-radius',
    });

    public constructor() {
        this.recipeForm = new FormGroup<RecipeFormData>({
            name: new FormControl<string>('', { nonNullable: true, validators: [Validators.required] }),
            description: new FormControl('', [Validators.maxLength(1000)]),
            prepTime: new FormControl(null, [Validators.required, Validators.min(1)]),
            cookTime: new FormControl(null, [Validators.required, Validators.min(1)]),
            servings: new FormControl(1, { nonNullable: true, validators: [Validators.required, Validators.min(1)] }),
            steps: new FormArray<FormGroup<FormGroupControls<StepFormValues>>>([], nonEmptyArrayValidator()),
        });

        this.addStep();
    }

    public ngOnInit(): void {
        const recipeData = this.getRecipeData();
        if (recipeData) {
            this.populateForm(recipeData);
        }
    }

    public get steps(): FormArray<FormGroup<FormGroupControls<StepFormValues>>> {
        return this.recipeForm.controls.steps;
    }

    public addStep(): void {
        this.steps.push(
            new FormGroup<StepFormData>({
                description: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
                ingredients: new FormArray<FormGroup<FormGroupControls<IngredientFormValues>>>([
                    new FormGroup<IngredientFormData>({
                        food: new FormControl<Food | null>(null, Validators.required),
                        amount: new FormControl(null, [Validators.required, Validators.min(0.01)]),
                    }),
                ], nonEmptyArrayValidator()),
            })
        );
    }

    public removeStep(index: number): void {
        this.steps.removeAt(index);
    }

    public getStepIngredients(stepIndex: number):  FormArray<FormGroup<FormGroupControls<IngredientFormValues>>> {
        const step = this.steps.at(stepIndex);
        return step.controls.ingredients;
    }

    public getFoodName(stepIndex: number, ingredientIndex: number): string {
        const ingredientsArray = this.getStepIngredients(stepIndex);
        const foodControl = ingredientsArray.at(ingredientIndex).controls.food;
        return foodControl?.value?.name || '';
    }

    public getFoodUnit(stepIndex: number, ingredientIndex: number): string | null {
        const ingredientsArray = this.getStepIngredients(stepIndex);
        const foodControl = ingredientsArray.at(ingredientIndex).controls.food;
        const unit = foodControl.value?.baseUnit;
        return unit
            ? `, ${this.translateService.instant('FOOD_AMOUNT_UNITS.' + unit.toUpperCase())}`
            : null;
    }

    public isFoodInvalid(stepIndex: number, ingredientIndex: number): boolean {
        const ingredientsArray = this.getStepIngredients(stepIndex);
        const foodControl = ingredientsArray.at(ingredientIndex).controls.food;
        return !!foodControl && foodControl.invalid && foodControl.touched;
    }

    public async onFoodSelectClick(stepIndex: number, ingredientIndex: number): Promise<void> {
        this.selectedStepIndex = stepIndex;
        this.selectedIngredientIndex = ingredientIndex;
        this.foodListDialog(null).subscribe({
            next: food => {
                const ingredientsArray = this.getStepIngredients(stepIndex);
                const foodGroup = ingredientsArray.at(ingredientIndex);
                foodGroup.patchValue({ food });
            },
        });
    }

    public addIngredientToStep(stepIndex: number): void {
        const step = this.steps.at(stepIndex);
        const ingredients = step.controls.ingredients;

        ingredients.push(
            new FormGroup<IngredientFormData>({
                food: new FormControl(null, [Validators.required]),
                amount: new FormControl(null, [Validators.required, Validators.min(0.01)]),
            })
        );
    }

    public removeIngredientFromStep(stepIndex: number, ingredientIndex: number): void {
        const step = this.steps.at(stepIndex);
        const ingredients = step.controls.ingredients;
        ingredients.removeAt(ingredientIndex);
    }

    private populateForm(recipeData: any): void {
        this.recipeForm.patchValue({
            name: recipeData.name,
            description: recipeData.description,
            prepTime: recipeData.prepTime,
            cookTime: recipeData.cookTime,
            servings: recipeData.servings,
        });

        recipeData.steps.forEach((step: any) => {
            const stepGroup = new FormGroup<StepFormData>({
                description: new FormControl(step.description, [Validators.required]),
                ingredients: new FormArray<FormGroup<FormGroupControls<IngredientFormValues>>>([]),
            });

            step.ingredients.forEach((ingredient: IngredientFormValues) => {
                (stepGroup.controls.ingredients).push(
                    new FormGroup<IngredientFormData>({
                        food: new FormControl(ingredient.food, [Validators.required]),
                        amount: new FormControl(ingredient.amount, [Validators.required, Validators.min(0.01)]),
                    })
                );
            });

            this.steps.push(stepGroup);
        });
    }

    public onSubmit(): void {
        this.markFormGroupTouched(this.recipeForm);

        if (this.recipeForm.valid) {
            const recipeData = this.prepareRecipeDto();
            const recipe = this.recipe();
            this.addRecipe(recipeData);
            /*recipe
                ? this.updateRecipe(recipeData.id, recipeData)
                : this.addRecipe(recipeData);*/
        }
    }

    private async addRecipe(recipeData: RecipeDto): Promise<void> {
        this.recipeService.create(recipeData).subscribe({
            next: response => this.handleSubmitResponse(response),
        });
    }

    private async handleSubmitResponse(response: ApiResponse<RecipeDto | null>): Promise<void> {
        if (response.status === 'success') {
            if (!this.recipe()) {
                this.recipeForm.reset();
            }
            //await this.showConfirmDialog();
        } else if (response.status === 'error') {
            this.handleSubmitError(response.error);
        }
    }

    private getRecipeData(): any {
        return null;
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

    private prepareRecipeDto(): RecipeDto {
        const formValue = this.recipeForm.value as RecipeFormValues;

        return {
            name: formValue.name,
            description: formValue.description || undefined,
            prepTime: formValue.prepTime ?? 0,
            cookTime: formValue.cookTime ?? 0,
            servings: formValue.servings,
            steps: formValue.steps.map(step => ({
                description: step.description,
                ingredients: step.ingredients.map(ingredient => ({
                    foodId: ingredient.food?.id ?? 0,
                    amount: ingredient.amount ?? 0,
                })),
            })),
        };

        /*return {
            name: formValue.name.value,
            description: formValue.description.value || undefined,
            prepTime: formValue.prepTime.value ?? 0,
            cookTime: formValue.cookTime.value ?? 0,
            servings: formValue.servings.value ?? 0,
            steps: formValue.steps.value.map(step => ({
                description: step.description!,
                ingredients: step.ingredients?.map(ingredient => ({
                    foodId: ingredient.food!.id,
                    amount: ingredient.amount!,
                } as RecipeIngredientDto)),
            } as RecipeStepDto)),
        };*/
    }

}

interface RecipeFormValues {
    name: string;
    description: string | null;
    prepTime: number | null;
    cookTime: number | null;
    servings: number;
    steps: StepFormValues[];
}

interface StepFormValues {
    description: string;
    ingredients: IngredientFormValues[];
}

interface IngredientFormValues {
    food: Food | null;
    amount: number | null;
}

type RecipeFormData = FormGroupControls<RecipeFormValues>;
type StepFormData = FormGroupControls<StepFormValues>;
type IngredientFormData = FormGroupControls<IngredientFormValues>;
