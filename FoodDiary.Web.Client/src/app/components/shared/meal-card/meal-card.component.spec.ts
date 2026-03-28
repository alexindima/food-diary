import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MealCardComponent, MealCardItem } from './meal-card.component';
import { TranslateModule } from '@ngx-translate/core';

describe('MealCardComponent', () => {
    let component: MealCardComponent;
    let fixture: ComponentFixture<MealCardComponent>;

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
        items: [{}, {}, {}],
        aiSessions: null,
    };

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [MealCardComponent, TranslateModule.forRoot()],
        }).compileComponents();

        fixture = TestBed.createComponent(MealCardComponent);
        component = fixture.componentInstance;
        fixture.componentRef.setInput('meal', mockMeal);
    });

    it('should create', () => {
        fixture.detectChanges();
        expect(component).toBeTruthy();
    });

    it('should display calories', () => {
        fixture.detectChanges();
        const el: HTMLElement = fixture.nativeElement;
        const caloriesEl = el.querySelector('.meal-card__calories-value');
        expect(caloriesEl?.textContent?.trim()).toBe('650');
    });

    it('should emit open on card click', () => {
        fixture.detectChanges();

        const openSpy = vi.fn();
        component.open.subscribe(openSpy);

        const el: HTMLElement = fixture.nativeElement;
        const card = el.querySelector<HTMLElement>('.meal-card');
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
});
