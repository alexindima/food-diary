import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RecipeService } from '../../../services/recipe.service';
import { FormArray, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { FormGroupControls } from '../../../types/common.data';
import { NgForOf } from "@angular/common";

@Component({
    selector: 'fd-recipe-manage',
    imports: [
        ReactiveFormsModule,
    ],
    templateUrl: './recipe-manage.component.html',
    styleUrl: './recipe-manage.component.less',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class RecipeManageComponent {
    /*private readonly recipeService = inject(RecipeService);

    public recipeForm: FormGroup<RecipeFormData>;

    public constructor() {
        this.recipeForm = new FormGroup<RecipeFormData>({
            name: new FormControl<string>('', { nonNullable: true, validators: Validators.required }),
            description: new FormControl<string | null>(null),
            category: new FormControl<string | null>(null),
            imageUrl: new FormControl<string | null>(null),
            preparationTimeInMin: new FormControl<number | null>(null),
            cookTimeInMin: new FormControl<number | null>(null),
            servings: new FormControl<number>(0, { nonNullable: true, validators: Validators.required }),
            ingredients: new FormArray<FormControl<IngredientFormValues>>([]),
            steps: new FormArray<FormControl<StepFormValues>>([]),
        });
    }

    // Геттеры для удобства работы с FormArray
    public get ingredients(): FormArray<FormControl<IngredientFormValues>> {
        return this.recipeForm.controls.ingredients;
    }

    public get steps(): FormArray<FormControl<StepFormValues>> {
        return this.recipeForm.controls.steps;
    }

    // Методы для управления ингредиентами
    public addIngredient(): void {
        const ingredientGroup = new FormGroup<FormGroupControls<IngredientFormValues>>({
            foodName: new FormControl<string>('', { nonNullable: true, validators: Validators.required }),
            amount: new FormControl<number>(0, { nonNullable: true, validators: Validators.required }),
            unit: new FormControl('G', { nonNullable: true, validators: Validators.required }),
        });
        //this.ingredients.push(ingredientGroup);
    }

    public removeIngredient(index: number): void {
        this.ingredients.removeAt(index);
    }

    // Методы для управления шагами
    public addStep(): void {
        const stepGroup = new FormGroup<FormGroupControls<StepFormValues>>({
            description: new FormControl('', { nonNullable: true, validators: Validators.required }),
            imageUrl: new FormControl(null),
        });
        //this.steps.push(stepGroup);
    }

    public removeStep(index: number): void {
        this.steps.removeAt(index);
    }

    // Отправка данных формы
    public onSubmit(): void {
        if (this.recipeForm.valid) {
            //const formData: RecipeFormValues = this.recipeForm.value;
            /!*this.recipeService.createRecipe(formData).subscribe({
                next: () => {
                    this.router.navigate(['/recipes']);
                },
                error: (err) => {
                    console.error('Ошибка при добавлении рецепта:', err);
                },
            });*!/
        }
    }*/
}

interface RecipeFormValues {
    name: string;
    description: string | null;
    category: string | null;
    imageUrl: string | null;
    preparationTimeInMin: number | null;
    cookTimeInMin: number | null;
    servings: number;
    ingredients: IngredientFormValues[];
    steps: StepFormValues[];
}

export interface IngredientFormValues {
    foodName: string;
    amount: number;
    unit: string;
}

export interface StepFormValues {
    description: string;
    imageUrl: string | null;
}

type RecipeFormData = FormGroupControls<RecipeFormValues>;

