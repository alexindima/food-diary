import { ChangeDetectionStrategy, Component, input, OnInit } from '@angular/core';
import { FormArray, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { FormGroupControls } from '../../../types/common.data';
import {
    TuiButton,
    TuiError,
    TuiLabel,
    TuiTextfieldComponent,
    TuiTextfieldDirective
} from '@taiga-ui/core';
import { TranslatePipe } from '@ngx-translate/core';
import { TuiInputNumberModule, TuiSelectModule, TuiTextareaModule } from '@taiga-ui/legacy';
import { CustomGroupComponent } from '../../shared/custom-group/custom-group.component';
import { Food } from '../../../types/food.data';
import { AsyncPipe, NgForOf } from '@angular/common';
import { TuiFieldErrorPipe } from '@taiga-ui/kit';

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
    ],
    templateUrl: './recipe-manage.component.html',
    styleUrl: './recipe-manage.component.less',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class RecipeManageComponent implements OnInit {
    public recipe = input<boolean>();

    public recipeForm: FormGroup<RecipeFormData>;

    public constructor() {
        this.recipeForm = new FormGroup<RecipeFormData>({
            name: new FormControl<string>('', {nonNullable: true, validators: [Validators.required]}),
            description: new FormControl('', [Validators.maxLength(1000)]),
            imageUrl: new FormControl(null),
            prepTime: new FormControl(null, [Validators.min(1)]),
            cookTime: new FormControl(null, [Validators.min(1)]),
            servings: new FormControl(1, {nonNullable: true, validators: [Validators.required, Validators.min(1)]}),
            steps: new FormArray<FormGroup<FormGroupControls<StepFormValues>>>([]),
        });

        this.addStep();

        this.recipeForm.valueChanges.subscribe(form => {
                console.log(form);
            }
        )
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
                description: new FormControl('', [Validators.required]),
                ingredients: new FormArray<FormGroup<FormGroupControls<IngredientFormValues>>>([
                    new FormGroup<IngredientFormData>({
                        food: new FormControl(null, [Validators.required]),
                        amount: new FormControl(null, [Validators.required, Validators.min(0.01)]),
                    }),
                ]),
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
            imageUrl: recipeData.imageUrl,
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
        if (this.recipeForm.valid) {
            const recipeData = this.recipeForm.value;
            console.log('Recipe Data:', recipeData);
        } else {
            this.recipeForm.markAllAsTouched();
        }
    }

    private getRecipeData(): any {
        return null;
    }
}

interface RecipeFormValues {
    name: string;
    description: string | null;
    imageUrl: string | null;
    prepTime: number | null;
    cookTime: number | null;
    servings: number;
    steps: StepFormValues[];
}

interface StepFormValues {
    description: string | null;
    ingredients: IngredientFormValues[];
}

interface IngredientFormValues {
    food: Food | null;
    amount: number | null;
}

type RecipeFormData = FormGroupControls<RecipeFormValues>;
type StepFormData = FormGroupControls<StepFormValues>;
type IngredientFormData = FormGroupControls<IngredientFormValues>;
