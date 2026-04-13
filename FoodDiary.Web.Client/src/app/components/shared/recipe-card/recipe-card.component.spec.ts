import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { RecipeCardComponent, RecipeCardItem } from './recipe-card.component';
import { TranslateModule } from '@ngx-translate/core';
import { Observable, of } from 'rxjs';
// eslint-disable-next-line no-restricted-imports
import { FavoriteRecipeService } from '../../../features/recipes/api/favorite-recipe.service';
import { AuthService } from '../../../services/auth.service';

describe('RecipeCardComponent', () => {
    let component: RecipeCardComponent;
    let fixture: ComponentFixture<RecipeCardComponent>;

    const mockRecipe: RecipeCardItem = {
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
        steps: [{ ingredients: [{}, {}, {}] }, { ingredients: [{}, {}] }],
    };

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [RecipeCardComponent, TranslateModule.forRoot()],
            providers: [
                {
                    provide: FavoriteRecipeService,
                    useValue: {
                        isFavorite: (): Observable<boolean> => of(false),
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

        fixture = TestBed.createComponent(RecipeCardComponent);
        component = fixture.componentInstance;
        fixture.componentRef.setInput('recipe', mockRecipe);
    });

    it('should create', () => {
        fixture.detectChanges();
        expect(component).toBeTruthy();
    });

    it('should display recipe name', () => {
        fixture.detectChanges();
        const el: HTMLElement = fixture.nativeElement;
        const titleEl = el.querySelector('.recipe-card__title');
        expect(titleEl?.textContent?.trim()).toBe('Test Recipe');
    });

    it('should emit open on card click', () => {
        fixture.detectChanges();

        const openSpy = vi.fn();
        component.open.subscribe(openSpy);

        const el: HTMLElement = fixture.nativeElement;
        const card = el.querySelector<HTMLElement>('.recipe-card');
        card?.click();

        expect(openSpy).toHaveBeenCalledOnce();
    });

    it('should emit addToMeal and stop propagation on add button click', () => {
        fixture.detectChanges();

        const addSpy = vi.fn();
        component.addToMeal.subscribe(addSpy);

        const mockEvent = new Event('click', { bubbles: true });
        const stopSpy = vi.spyOn(mockEvent, 'stopPropagation');

        component.handleAdd(mockEvent);

        expect(addSpy).toHaveBeenCalledOnce();
        expect(stopSpy).toHaveBeenCalledOnce();
    });

    it('should calculate total time as sum of prepTime and cookTime', () => {
        fixture.detectChanges();
        expect(component.getTotalTime()).toBe(45);
    });

    it('should return null for total time when both prepTime and cookTime are 0', () => {
        fixture.componentRef.setInput('recipe', {
            ...mockRecipe,
            prepTime: 0,
            cookTime: 0,
        });
        fixture.detectChanges();
        expect(component.getTotalTime()).toBeNull();
    });

    it('should return null for total time when prepTime and cookTime are null', () => {
        fixture.componentRef.setInput('recipe', {
            ...mockRecipe,
            prepTime: null,
            cookTime: null,
        });
        fixture.detectChanges();
        expect(component.getTotalTime()).toBeNull();
    });

    it('should handle only prepTime set', () => {
        fixture.componentRef.setInput('recipe', {
            ...mockRecipe,
            prepTime: 20,
            cookTime: null,
        });
        fixture.detectChanges();
        expect(component.getTotalTime()).toBe(20);
    });

    it('should count ingredients across all steps', () => {
        fixture.detectChanges();
        expect(component.getIngredientCount()).toBe(5);
    });

    it('should return 0 ingredients when steps is null', () => {
        fixture.componentRef.setInput('recipe', {
            ...mockRecipe,
            steps: null,
        });
        fixture.detectChanges();
        expect(component.getIngredientCount()).toBe(0);
    });

    it('should return 0 ingredients when steps is empty', () => {
        fixture.componentRef.setInput('recipe', {
            ...mockRecipe,
            steps: [],
        });
        fixture.detectChanges();
        expect(component.getIngredientCount()).toBe(0);
    });

    it('should handle steps with null ingredients', () => {
        fixture.componentRef.setInput('recipe', {
            ...mockRecipe,
            steps: [{ ingredients: null }, { ingredients: [{}, {}] }],
        });
        fixture.detectChanges();
        expect(component.getIngredientCount()).toBe(2);
    });

    it('should display calories', () => {
        fixture.detectChanges();
        const el: HTMLElement = fixture.nativeElement;
        const caloriesEl = el.querySelector('.recipe-card__calories-value');
        expect(caloriesEl?.textContent?.trim()).toBe('368');
    });
});
