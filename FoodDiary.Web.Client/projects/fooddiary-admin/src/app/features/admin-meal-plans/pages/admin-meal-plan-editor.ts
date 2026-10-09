import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, inject, input, output, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { applyEach, form, FormField, FormRoot, max, maxLength, min, pattern, required } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiCheckboxComponent, FdUiInputComponent, FdUiSelectComponent, FdUiTextareaComponent } from 'fd-ui-kit';
import { finalize, type Subscription } from 'rxjs';

import { adminId } from '../../../shared/models/semantics/admin-meaning';
import { AdminMealPlansFacade } from '../lib/admin-meal-plans.facade';
import {
    CATALOG_DIETS,
    CATALOG_MEALS,
    type CatalogDay,
    type CatalogPlan,
    type CatalogPlanRequest,
    type CatalogRecipe,
} from '../models/admin-meal-plan.data';

const MAX_DAYS = 31;
const MAX_MEALS = 20;
const MAX_SERVINGS = 100;
const NAME_LENGTH = 256;
const DESCRIPTION_LENGTH = 2048;
type EditorModel = Omit<CatalogPlanRequest, 'description'> & { description: string };
const emptyModel = (): EditorModel => ({
    name: '',
    description: '',
    dietType: 'Balanced',
    durationDays: 1,
    targetCaloriesPerDay: null,
    isPublished: false,
    days: [{ dayNumber: 1, meals: [] }],
});

@Component({
    selector: 'fd-admin-meal-plan-editor',
    imports: [
        FormField,
        FormRoot,
        TranslatePipe,
        FdUiButtonComponent,
        FdUiInputComponent,
        FdUiSelectComponent,
        FdUiTextareaComponent,
        FdUiCheckboxComponent,
    ],
    templateUrl: './admin-meal-plan-editor.html',
    styleUrl: './admin-meal-plans.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminMealPlanEditorComponent {
    public readonly plan = input<CatalogPlan | null>(null);
    public readonly saved = output();
    public readonly cancelled = output();
    private readonly api = inject(AdminMealPlansFacade);
    private readonly destroyRef = inject(DestroyRef);
    private readonly translate = inject(TranslateService);
    private recipeRequest?: Subscription;
    protected readonly saving = signal(false);
    protected readonly error = signal<string | null>(null);
    protected readonly recipes = signal<CatalogRecipe[]>([]);
    protected readonly recipeSearch = signal('');
    protected readonly model = signal<EditorModel>(emptyModel());
    private readonly duration = computed(() => this.model().durationDays);
    protected readonly form = form(this.model, path => {
        required(path.name);
        maxLength(path.name, NAME_LENGTH);
        pattern(path.name, /\S/);
        maxLength(path.description, DESCRIPTION_LENGTH);
        required(path.dietType);
        required(path.durationDays);
        min(path.durationDays, 1);
        max(path.durationDays, MAX_DAYS);
        min(path.targetCaloriesPerDay, 1);
        applyEach(path.days, day => {
            applyEach(day.meals, meal => {
                required(meal.mealType);
                required(meal.recipeId);
                required(meal.servings);
                min(meal.servings, 1);
                max(meal.servings, MAX_SERVINGS);
            });
        });
    });

    public constructor() {
        effect(() => {
            const plan = this.plan();
            untracked(() => {
                this.open(plan);
            });
        });
        effect(() => {
            const duration = this.duration();
            untracked(() => {
                this.resizeDays(duration);
            });
        });
    }

    private open(plan: CatalogPlan | null): void {
        this.error.set(null);
        if (plan === null) {
            this.model.set(emptyModel());
            this.recipes.set([]);
        } else {
            this.model.set({
                name: plan.name,
                description: plan.description ?? '',
                dietType: plan.dietType,
                durationDays: plan.durationDays,
                targetCaloriesPerDay: plan.targetCaloriesPerDay,
                isPublished: plan.isCurated,
                days: Array.from({ length: plan.durationDays }, (_, index) => ({
                    dayNumber: index + 1,
                    meals: (plan.days.find(day => day.dayNumber === index + 1)?.meals ?? []).map(meal => ({ ...meal })),
                })),
            });
            this.recipes.set(
                plan.days.flatMap(day =>
                    day.meals.map(meal => ({ id: meal.recipeId, name: meal.recipeName ?? meal.recipeId, servings: meal.servings })),
                ),
            );
        }
        this.searchRecipes();
    }

    private resizeDays(duration: number): void {
        if (!Number.isInteger(duration) || duration < 1 || duration > MAX_DAYS) {
            return;
        }
        this.model.update(model => ({
            ...model,
            days: Array.from({ length: duration }, (_, index) => model.days[index] ?? { dayNumber: index + 1, meals: [] }),
        }));
    }

    protected addMeal(dayIndex: number): void {
        this.updateMeals(dayIndex, day => ({
            ...day,
            meals:
                day.meals.length < MAX_MEALS
                    ? [...day.meals, { mealType: 'Breakfast', recipeId: adminId<'recipe'>(''), servings: 1 }]
                    : day.meals,
        }));
    }
    protected removeMeal(dayIndex: number, mealIndex: number): void {
        this.updateMeals(dayIndex, day => ({ ...day, meals: day.meals.filter((_, index) => index !== mealIndex) }));
    }
    private updateMeals(dayIndex: number, update: (day: CatalogDay) => CatalogDay): void {
        this.model.update(model => ({ ...model, days: model.days.map((day, index) => (index === dayIndex ? update(day) : day)) }));
    }
    protected dietOptions(): Array<{ value: string; label: string }> {
        return CATALOG_DIETS.map(value => ({
            value,
            label: String(this.translate.instant(`ADMIN_MEAL_PLANS.DIET_TYPE.${value.toUpperCase()}`)),
        }));
    }
    protected mealOptions(): Array<{ value: string; label: string }> {
        return CATALOG_MEALS.map(value => ({
            value,
            label: String(this.translate.instant(`ADMIN_MEAL_PLANS.MEAL_TYPE.${value.toUpperCase()}`)),
        }));
    }
    protected recipeOptions(): Array<{ value: string; label: string }> {
        return this.recipes().map(recipe => ({ value: recipe.id, label: recipe.name }));
    }

    protected searchRecipes(): void {
        this.recipeRequest?.unsubscribe();
        this.recipeRequest = this.api
            .recipes(this.recipeSearch())
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: recipes => {
                    const selected = new Set(this.model().days.flatMap(day => day.meals.map(meal => meal.recipeId)));
                    this.recipes.update(current => [
                        ...new Map(
                            [...current.filter(recipe => selected.has(recipe.id)), ...recipes].map(recipe => [recipe.id, recipe]),
                        ).values(),
                    ]);
                },
                error: () => {
                    this.error.set(String(this.translate.instant('ADMIN_MEAL_PLANS.RECIPE_ERROR')));
                },
            });
    }
    protected cancel(): void {
        this.cancelled.emit();
    }

    protected save(): void {
        this.error.set(null);
        if (this.form().invalid() || this.saving()) {
            this.error.set(String(this.translate.instant('ADMIN_MEAL_PLANS.INVALID')));
            return;
        }
        const value = this.model();
        if (!Number.isInteger(value.durationDays) || value.days.some(day => day.meals.some(meal => !Number.isInteger(meal.servings)))) {
            this.error.set(String(this.translate.instant('ADMIN_MEAL_PLANS.INVALID')));
            return;
        }
        if (value.isPublished && value.days.some(day => day.meals.length === 0)) {
            this.error.set(String(this.translate.instant('ADMIN_MEAL_PLANS.INCOMPLETE')));
            return;
        }
        this.saving.set(true);
        const description = value.description.trim();
        this.api
            .save(this.plan()?.id ?? null, { ...value, description: description.length > 0 ? description : null })
            .pipe(
                takeUntilDestroyed(this.destroyRef),
                finalize(() => {
                    this.saving.set(false);
                }),
            )
            .subscribe({
                next: () => {
                    this.saved.emit();
                },
                error: () => {
                    this.error.set(String(this.translate.instant('ADMIN_MEAL_PLANS.SAVE_ERROR')));
                },
            });
    }
}
