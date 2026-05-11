import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { type Observable, of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

// eslint-disable-next-line no-restricted-imports -- shared card spec needs the concrete feature favorite service token
import { FavoriteRecipeService } from '../../../features/recipes/api/favorite-recipe.service';
import { AuthService } from '../../../services/auth.service';
import { RecipeCardComponent, type RecipeCardItem } from './recipe-card.component';

const TOTAL_TIME_MINUTES = 45;
const PREP_TIME_ONLY_MINUTES = 20;
const INGREDIENT_COUNT = 5;
const MOCK_RECIPE: RecipeCardItem = {
    id: 'recipe-1',
    name: 'Test Recipe',
    isOwnedByCurrentUser: true,
    prepTime: 15,
    cookTime: 30,
    totalProteins: 25,
    totalFats: 12,
    totalCarbs: 40,
    totalFiber: 6,
    totalAlcohol: 0,
    totalCalories: 368,
    qualityScore: 64,
    qualityGrade: 'yellow',
    steps: [{ ingredients: [{}, {}, {}] }, { ingredients: [{}, {}] }],
};

type RecipeCardTestContext = {
    component: RecipeCardComponent;
    el: HTMLElement;
    fixture: ComponentFixture<RecipeCardComponent>;
};

async function setupRecipeCardAsync(): Promise<RecipeCardTestContext> {
    await TestBed.configureTestingModule({
        imports: [RecipeCardComponent, TranslateModule.forRoot()],
        providers: [
            {
                provide: FavoriteRecipeService,
                useValue: {
                    add: (): Observable<{ id: string }> => of({ id: 'favorite-recipe-1' }),
                    remove: (): Observable<void> => of(void 0),
                    getAll: (): Observable<[]> => of([]),
                },
            },
            {
                provide: AuthService,
                useValue: {
                    isAuthenticated: signal(true),
                },
            },
        ],
    }).compileComponents();

    const fixture = TestBed.createComponent(RecipeCardComponent);
    const component = fixture.componentInstance;
    fixture.componentRef.setInput('recipe', MOCK_RECIPE);
    fixture.componentRef.setInput('imageUrl', null);
    const el = fixture.nativeElement as HTMLElement;

    return { component, el, fixture };
}

describe('RecipeCardComponent', () => {
    it('should create', async () => {
        const { component, fixture } = await setupRecipeCardAsync();
        fixture.detectChanges();
        expect(component).toBeTruthy();
    });
});

describe('RecipeCardComponent content', () => {
    it('should display recipe name', async () => {
        const { el, fixture } = await setupRecipeCardAsync();
        fixture.detectChanges();

        const titleEl = el.querySelector('.entity-card__name');
        expect(titleEl?.textContent.trim()).toBe('Test Recipe');
    });

    it('should display calories', async () => {
        const { el, fixture } = await setupRecipeCardAsync();
        fixture.detectChanges();

        const caloriesEl = el.querySelector('.entity-card__calories-value');
        expect(caloriesEl?.textContent.trim()).toBe('368');
    });

    it('should display quality score progress', async () => {
        const { el, fixture } = await setupRecipeCardAsync();
        fixture.detectChanges();

        const labelEl = el.querySelector('.entity-card__quality-label');
        const valueEl = el.querySelector('.entity-card__quality-value');
        const fillEl = el.querySelector<HTMLElement>('.entity-card__quality-fill');

        expect(labelEl?.textContent.trim()).toBe('PRODUCT_CARD.QUALITY_SCORE');
        expect(valueEl?.textContent.trim()).toBe('64');
        expect(fillEl?.style.width).toBe('64%');
    });
});

describe('RecipeCardComponent events', () => {
    it('should emit open on card click', async () => {
        const { component, el, fixture } = await setupRecipeCardAsync();
        fixture.detectChanges();

        const openSpy = vi.fn();
        component.open.subscribe(openSpy);

        const card = el.querySelector<HTMLElement>('.entity-card');
        card?.click();

        expect(openSpy).toHaveBeenCalledOnce();
    });

    it('should emit addToMeal on add button click', async () => {
        const { component, fixture } = await setupRecipeCardAsync();
        fixture.detectChanges();

        const addSpy = vi.fn();
        component.addToMeal.subscribe(addSpy);

        component.handleAdd();

        expect(addSpy).toHaveBeenCalledOnce();
    });
});

describe('RecipeCardComponent total time', () => {
    it('should calculate total time as sum of prepTime and cookTime', async () => {
        const { component, fixture } = await setupRecipeCardAsync();
        fixture.detectChanges();
        expect(component.totalTime()).toBe(TOTAL_TIME_MINUTES);
    });

    it('should return null for total time when both prepTime and cookTime are 0', async () => {
        const { component, fixture } = await setupRecipeCardAsync();
        fixture.componentRef.setInput('recipe', {
            ...MOCK_RECIPE,
            prepTime: 0,
            cookTime: 0,
        });
        fixture.detectChanges();
        expect(component.totalTime()).toBeNull();
    });

    it('should return null for total time when prepTime and cookTime are null', async () => {
        const { component, fixture } = await setupRecipeCardAsync();
        fixture.componentRef.setInput('recipe', {
            ...MOCK_RECIPE,
            prepTime: null,
            cookTime: null,
        });
        fixture.detectChanges();
        expect(component.totalTime()).toBeNull();
    });

    it('should handle only prepTime set', async () => {
        const { component, fixture } = await setupRecipeCardAsync();
        fixture.componentRef.setInput('recipe', {
            ...MOCK_RECIPE,
            prepTime: PREP_TIME_ONLY_MINUTES,
            cookTime: null,
        });
        fixture.detectChanges();
        expect(component.totalTime()).toBe(PREP_TIME_ONLY_MINUTES);
    });
});

describe('RecipeCardComponent ingredients', () => {
    it('should count ingredients across all steps', async () => {
        const { component, fixture } = await setupRecipeCardAsync();
        fixture.detectChanges();
        expect(component.ingredientCount()).toBe(INGREDIENT_COUNT);
    });

    it('should return 0 ingredients when steps is null', async () => {
        const { component, fixture } = await setupRecipeCardAsync();
        fixture.componentRef.setInput('recipe', {
            ...MOCK_RECIPE,
            steps: null,
        });
        fixture.detectChanges();
        expect(component.ingredientCount()).toBe(0);
    });

    it('should return 0 ingredients when steps is empty', async () => {
        const { component, fixture } = await setupRecipeCardAsync();
        fixture.componentRef.setInput('recipe', {
            ...MOCK_RECIPE,
            steps: [],
        });
        fixture.detectChanges();
        expect(component.ingredientCount()).toBe(0);
    });

    it('should handle steps with null ingredients', async () => {
        const { component, fixture } = await setupRecipeCardAsync();
        fixture.componentRef.setInput('recipe', {
            ...MOCK_RECIPE,
            steps: [{ ingredients: null }, { ingredients: [{}, {}] }],
        });
        fixture.detectChanges();
        expect(component.ingredientCount()).toBe(2);
    });
});
