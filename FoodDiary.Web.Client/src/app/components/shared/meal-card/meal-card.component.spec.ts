import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { MealCardComponent, MealCardItem } from './meal-card.component';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { Observable, of } from 'rxjs';
// eslint-disable-next-line no-restricted-imports
import { FavoriteMealService } from '../../../features/meals/api/favorite-meal.service';
import { AuthService } from '../../../services/auth.service';

describe('MealCardComponent', () => {
    let component: MealCardComponent;
    let fixture: ComponentFixture<MealCardComponent>;
    let translateService: TranslateService;

    const mockMeal: MealCardItem = {
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

    beforeEach(async () => {
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

        fixture = TestBed.createComponent(MealCardComponent);
        component = fixture.componentInstance;
        translateService = TestBed.inject(TranslateService);
        fixture.componentRef.setInput('meal', mockMeal);
    });

    it('should create', () => {
        fixture.detectChanges();
        expect(component).toBeTruthy();
    });

    it('should display calories', () => {
        fixture.detectChanges();
        const el: HTMLElement = fixture.nativeElement;
        const caloriesEl = el.querySelector('.entity-card__calories-value');
        expect(caloriesEl?.textContent?.trim()).toBe('650');
    });

    it('should emit open on card click', () => {
        fixture.detectChanges();

        const openSpy = vi.fn();
        component.open.subscribe(openSpy);

        const el: HTMLElement = fixture.nativeElement;
        const card = el.querySelector<HTMLElement>('.entity-card');
        card?.click();

        expect(openSpy).toHaveBeenCalledOnce();
    });

    it('should calculate itemCount from items array', () => {
        fixture.detectChanges();
        expect(component.itemCount()).toBe(3);
    });

    it('should calculate itemCount as 0 when items is null', () => {
        fixture.componentRef.setInput('meal', { ...mockMeal, items: null });
        fixture.detectChanges();
        expect(component.itemCount()).toBe(0);
    });

    it('should include aiSession items in itemCount', () => {
        fixture.componentRef.setInput('meal', {
            ...mockMeal,
            items: [{}, {}],
            aiSessions: [{ items: [{}, {}, {}] }, { items: [{}] }],
        });
        fixture.detectChanges();
        expect(component.itemCount()).toBe(6);
    });

    it('should handle aiSessions with null items', () => {
        fixture.componentRef.setInput('meal', {
            ...mockMeal,
            items: [{}],
            aiSessions: [{ items: null }, null],
        });
        fixture.detectChanges();
        expect(component.itemCount()).toBe(1);
    });

    it('should compute coverImage with meal type stub when no imageUrl', () => {
        fixture.componentRef.setInput('meal', { ...mockMeal, imageUrl: null, mealType: 'LUNCH' });
        fixture.detectChanges();
        expect(component.coverImage()).toBe('assets/images/stubs/meals/lunch.svg');
    });

    it('should compute coverImage as fallback when no imageUrl and no mealType', () => {
        fixture.componentRef.setInput('meal', { ...mockMeal, imageUrl: null, mealType: null });
        fixture.detectChanges();
        expect(component.coverImage()).toBe('assets/images/stubs/meals/other.svg');
    });

    it('should resolve meal title when api returns mixed-case mealType', () => {
        const instantSpy = vi
            .spyOn(translateService, 'instant')
            .mockImplementation(key => (key === 'MEAL_CARD.MEAL_TYPES.DINNER' ? 'Dinner' : String(key)));

        fixture.componentRef.setInput('meal', { ...mockMeal, mealType: 'Dinner' });
        fixture.detectChanges();

        expect(component.mealTitle()).toBe('Dinner');
        expect(instantSpy).toHaveBeenCalledWith('MEAL_CARD.MEAL_TYPES.DINNER');
    });

    it('should display quality score progress', () => {
        fixture.detectChanges();

        const el: HTMLElement = fixture.nativeElement;
        const labelEl = el.querySelector('.entity-card__quality-label');
        const valueEl = el.querySelector('.entity-card__quality-value');
        const fillEl = el.querySelector<HTMLElement>('.entity-card__quality-fill');

        expect(labelEl?.textContent?.trim()).toBe('PRODUCT_CARD.QUALITY_SCORE');
        expect(valueEl?.textContent?.trim()).toBe('42');
        expect(fillEl?.style.width).toBe('42%');
    });

    it('should apply red quality fill class for low quality score', () => {
        fixture.componentRef.setInput('meal', {
            ...mockMeal,
            qualityScore: 26,
            qualityGrade: 'red',
        });
        fixture.detectChanges();

        const el: HTMLElement = fixture.nativeElement;
        const fillEl = el.querySelector<HTMLElement>('.entity-card__quality-fill');

        expect(fillEl?.classList.contains('entity-card__quality-fill--red')).toBe(true);
        expect(fillEl?.style.width).toBe('26%');
    });
});
