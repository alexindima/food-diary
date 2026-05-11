import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import type { FdUiImagePreviewDialogData } from 'fd-ui-kit/image-preview-dialog/fd-ui-image-preview-dialog.component';
import { type Observable, of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

// eslint-disable-next-line no-restricted-imports -- shared card spec needs the concrete feature favorite service token
import { FavoriteMealService } from '../../../features/meals/api/favorite-meal.service';
import { AuthService } from '../../../services/auth.service';
import { MealCardComponent, type MealCardItem } from './meal-card.component';

const ITEM_COUNT = 3;
const AI_ITEM_COUNT = 6;
const MOCK_MEAL: MealCardItem = {
    id: 'meal-1',
    date: '2026-03-28T12:30:00',
    mealType: 'LUNCH',
    totalCalories: 650,
    totalProteins: 30,
    totalFats: 20,
    totalCarbs: 80,
    totalFiber: 5,
    totalAlcohol: 0,
    qualityScore: 42,
    qualityGrade: 'yellow',
    items: [{}, {}, {}],
    aiSessions: null,
};

type MealCardTestContext = {
    component: MealCardComponent;
    dialogService: FdUiDialogService;
    el: HTMLElement;
    fixture: ComponentFixture<MealCardComponent>;
    translateService: TranslateService;
};

async function setupMealCardAsync(): Promise<MealCardTestContext> {
    await TestBed.configureTestingModule({
        imports: [MealCardComponent, TranslateModule.forRoot()],
        providers: [
            {
                provide: FavoriteMealService,
                useValue: {
                    add: (): Observable<{ id: string }> => of({ id: 'favorite-meal-1' }),
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

    const fixture = TestBed.createComponent(MealCardComponent);
    const component = fixture.componentInstance;
    const translateService = TestBed.inject(TranslateService);
    const dialogService = TestBed.inject(FdUiDialogService);
    fixture.componentRef.setInput('meal', MOCK_MEAL);
    const el = fixture.nativeElement as HTMLElement;

    return { component, dialogService, el, fixture, translateService };
}

describe('MealCardComponent', () => {
    it('should create', async () => {
        const { component, fixture } = await setupMealCardAsync();
        fixture.detectChanges();
        expect(component).toBeTruthy();
    });
});

describe('MealCardComponent content', () => {
    it('should display calories', async () => {
        const { el, fixture } = await setupMealCardAsync();
        fixture.detectChanges();

        const caloriesEl = el.querySelector('.entity-card__calories-value');
        expect(caloriesEl?.textContent.trim()).toBe('650');
    });

    it('should resolve meal title when api returns mixed-case mealType', async () => {
        const { component, fixture, translateService } = await setupMealCardAsync();
        const instantSpy = vi
            .spyOn(translateService, 'instant')
            .mockImplementation(key => (key === 'MEAL_CARD.MEAL_TYPES.DINNER' ? 'Dinner' : String(key)));

        fixture.componentRef.setInput('meal', { ...MOCK_MEAL, mealType: 'Dinner' });
        fixture.detectChanges();

        expect(component.mealTitle()).toBe('Dinner');
        expect(instantSpy).toHaveBeenCalledWith('MEAL_CARD.MEAL_TYPES.DINNER');
    });
});

describe('MealCardComponent events', () => {
    it('should emit open on card click', async () => {
        const { component, el, fixture } = await setupMealCardAsync();
        fixture.detectChanges();

        const openSpy = vi.fn();
        component.open.subscribe(openSpy);

        const card = el.querySelector<HTMLElement>('.entity-card');
        card?.click();

        expect(openSpy).toHaveBeenCalledOnce();
    });
});

describe('MealCardComponent item count', () => {
    it('should calculate itemCount from items array', async () => {
        const { component, fixture } = await setupMealCardAsync();
        fixture.detectChanges();
        expect(component.itemCount()).toBe(ITEM_COUNT);
    });

    it('should calculate itemCount as 0 when items is null', async () => {
        const { component, fixture } = await setupMealCardAsync();
        fixture.componentRef.setInput('meal', { ...MOCK_MEAL, items: null });
        fixture.detectChanges();
        expect(component.itemCount()).toBe(0);
    });

    it('should include aiSession items in itemCount', async () => {
        const { component, fixture } = await setupMealCardAsync();
        fixture.componentRef.setInput('meal', {
            ...MOCK_MEAL,
            items: [{}, {}],
            aiSessions: [{ items: [{}, {}, {}] }, { items: [{}] }],
        });
        fixture.detectChanges();
        expect(component.itemCount()).toBe(AI_ITEM_COUNT);
    });

    it('should handle aiSessions with null items', async () => {
        const { component, fixture } = await setupMealCardAsync();
        fixture.componentRef.setInput('meal', {
            ...MOCK_MEAL,
            items: [{}],
            aiSessions: [{ items: null }, null],
        });
        fixture.detectChanges();
        expect(component.itemCount()).toBe(1);
    });
});

describe('MealCardComponent cover image', () => {
    it('should compute coverImage with meal type stub when no imageUrl', async () => {
        const { component, fixture } = await setupMealCardAsync();
        fixture.componentRef.setInput('meal', { ...MOCK_MEAL, imageUrl: null, mealType: 'LUNCH' });
        fixture.detectChanges();
        expect(component.coverImage()).toBe('assets/images/stubs/meals/lunch.svg');
    });

    it('should use item image as cover when no meal image exists', async () => {
        const { component, fixture } = await setupMealCardAsync();
        fixture.componentRef.setInput('meal', {
            ...MOCK_MEAL,
            imageUrl: null,
            items: [{ product: { imageUrl: 'https://example.com/product.jpg', name: 'Product' } }],
        });
        fixture.detectChanges();

        expect(component.coverImage()).toBe('https://example.com/product.jpg');
        expect(component.collageImages()).toEqual([]);
        expect(component.hasPreviewImage()).toBe(true);
    });

    it('should compute coverImage as fallback when no imageUrl and no mealType', async () => {
        const { component, fixture } = await setupMealCardAsync();
        fixture.componentRef.setInput('meal', { ...MOCK_MEAL, imageUrl: null, mealType: null });
        fixture.detectChanges();
        expect(component.coverImage()).toBe('assets/images/stubs/meals/other.svg');
    });
});

describe('MealCardComponent collage images', () => {
    it('should build collage images from product and recipe items', async () => {
        const { component, fixture } = await setupMealCardAsync();
        fixture.componentRef.setInput('meal', createItemCollageMeal());
        fixture.detectChanges();

        expect(component.coverImage()).toBeNull();
        expect(component.collageImages()).toEqual([
            { url: 'https://example.com/product.jpg', alt: 'Product' },
            { url: 'https://example.com/recipe.jpg', alt: 'Recipe' },
        ]);
        expect(component.hasPreviewImage()).toBe(true);
    });

    it('should open collage preview when meal uses item collage images', async () => {
        const { component, dialogService, fixture } = await setupMealCardAsync();
        const dialogRef = Object.create(null) as ReturnType<FdUiDialogService['open']>;
        const openSpy = vi.spyOn(dialogService, 'open').mockImplementation(() => dialogRef);
        fixture.componentRef.setInput('meal', createItemCollageMeal());
        fixture.detectChanges();

        component.handlePreview();

        const call = openSpy.mock.calls[0] as Parameters<FdUiDialogService['open']> | undefined;
        const data = call?.[1]?.data as FdUiImagePreviewDialogData | undefined;

        expect(data?.imageUrl).toBeUndefined();
        expect(data?.collageImages).toEqual([
            { url: 'https://example.com/product.jpg', alt: 'Product' },
            { url: 'https://example.com/recipe.jpg', alt: 'Recipe' },
        ]);
    });

    it('should build collage images from multiple AI session images', async () => {
        const { component, fixture } = await setupMealCardAsync();
        fixture.componentRef.setInput('meal', {
            ...MOCK_MEAL,
            imageUrl: null,
            aiSessions: [
                { imageUrl: 'https://example.com/ai-meal-1.jpg', notes: 'First photo', items: [{}] },
                { imageUrl: 'https://example.com/ai-meal-2.jpg', notes: 'Second photo', items: [{}] },
            ],
        });
        fixture.detectChanges();

        expect(component.coverImage()).toBeNull();
        expect(component.collageImages()).toEqual([
            { url: 'https://example.com/ai-meal-1.jpg', alt: 'First photo' },
            { url: 'https://example.com/ai-meal-2.jpg', alt: 'Second photo' },
        ]);
        expect(component.hasPreviewImage()).toBe(true);
    });
});

describe('MealCardComponent AI images', () => {
    it('should use AI session image when meal imageUrl is empty', async () => {
        const { component, fixture } = await setupMealCardAsync();
        fixture.componentRef.setInput('meal', {
            ...MOCK_MEAL,
            imageUrl: null,
            aiSessions: [{ imageUrl: 'https://example.com/ai-meal.jpg', items: [{}] }],
        });
        fixture.detectChanges();
        expect(component.coverImage()).toBe('https://example.com/ai-meal.jpg');
        expect(component.collageImages()).toEqual([]);
        expect(component.hasPreviewImage()).toBe(true);
    });

    it('should keep explicit meal image over AI collage images', async () => {
        const { component, fixture } = await setupMealCardAsync();
        fixture.componentRef.setInput('meal', {
            ...MOCK_MEAL,
            imageUrl: 'https://example.com/meal-cover.jpg',
            aiSessions: [
                { imageUrl: 'https://example.com/ai-meal-1.jpg', items: [{}] },
                { imageUrl: 'https://example.com/ai-meal-2.jpg', items: [{}] },
            ],
        });
        fixture.detectChanges();

        expect(component.coverImage()).toBe('https://example.com/meal-cover.jpg');
        expect(component.collageImages()).toEqual([]);
    });
});

describe('MealCardComponent quality', () => {
    it('should display quality score progress', async () => {
        const { el, fixture } = await setupMealCardAsync();
        fixture.detectChanges();

        const labelEl = el.querySelector('.entity-card__quality-label');
        const valueEl = el.querySelector('.entity-card__quality-value');
        const fillEl = el.querySelector<HTMLElement>('.entity-card__quality-fill');

        expect(labelEl?.textContent.trim()).toBe('PRODUCT_CARD.QUALITY_SCORE');
        expect(valueEl?.textContent.trim()).toBe('42');
        expect(fillEl?.style.width).toBe('42%');
    });

    it('should apply red quality fill class for low quality score', async () => {
        const { el, fixture } = await setupMealCardAsync();
        fixture.componentRef.setInput('meal', {
            ...MOCK_MEAL,
            qualityScore: 26,
            qualityGrade: 'red',
        });
        fixture.detectChanges();

        const fillEl = el.querySelector<HTMLElement>('.entity-card__quality-fill');

        expect(fillEl?.classList.contains('entity-card__quality-fill--red')).toBe(true);
        expect(fillEl?.style.width).toBe('26%');
    });
});

function createItemCollageMeal(): MealCardItem {
    return {
        ...MOCK_MEAL,
        imageUrl: null,
        items: [
            { product: { imageUrl: 'https://example.com/product.jpg', name: 'Product' } },
            { recipe: { imageUrl: 'https://example.com/recipe.jpg', name: 'Recipe' } },
        ],
    };
}
