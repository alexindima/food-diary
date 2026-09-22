import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { FdUiButtonComponent } from 'fd-ui-kit';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { NutrientBadgesComponent } from '../../../../components/shared/nutrient-badges/nutrient-badges';
import type { FavoriteMeal } from '../../models/meal.data';
import { FavoriteMealRowComponent } from './favorite-meal-row';

const meal: FavoriteMeal = {
    id: 'f1',
    mealId: 'm1',
    name: 'My lunch',
    mealType: 'Lunch',
    itemNames: ['Rice', 'Chicken'],
    imageUrl: 'https://example.com/meal.jpg',
    createdAtUtc: '',
    mealDate: '',
    totalCalories: 500,
    totalProteins: 30,
    totalFats: 10,
    totalCarbs: 50,
    totalFiber: 7.5,
    itemCount: 2,
};

describe('FavoriteMealRowComponent', () => {
    beforeEach(() => {
        TestBed.configureTestingModule({ imports: [FavoriteMealRowComponent], providers: [provideTranslateTesting()] });
    });
    function create(item: FavoriteMeal = meal): ComponentFixture<FavoriteMealRowComponent> {
        const fixture = TestBed.createComponent(FavoriteMealRowComponent);
        fixture.componentRef.setInput('meal', item);
        fixture.detectChanges();
        return fixture;
    }
    it('shows the name and composition separately and forwards all four nutrient values', () => {
        const fixture = create();
        expect((fixture.nativeElement as HTMLElement).textContent).toContain('My lunch');
        expect((fixture.nativeElement as HTMLElement).textContent).toContain('Rice, Chicken');
        const badges = fixture.debugElement.query(By.directive(NutrientBadgesComponent)).componentInstance as NutrientBadgesComponent;
        expect([badges.proteins(), badges.fats(), badges.carbs(), badges.fiber()]).toEqual([
            meal.totalProteins,
            meal.totalFats,
            meal.totalCarbs,
            meal.totalFiber,
        ]);
        expect((fixture.nativeElement as HTMLElement).querySelector('img')?.getAttribute('src')).toBe(meal.imageUrl);
    });
    it('uses the meal type without hiding composition when the custom name is blank', () => {
        const fixture = create({ ...meal, name: '  ' });
        expect((fixture.nativeElement as HTMLElement).textContent).toContain('MEAL_TYPES.LUNCH');
        expect((fixture.nativeElement as HTMLElement).textContent).toContain('Rice, Chicken');
    });
    it('falls back for missing or failed photos and restores a different valid photo', () => {
        const fixture = create();
        const image = (): HTMLImageElement => (fixture.nativeElement as HTMLElement).querySelector('img') as HTMLImageElement;
        image().dispatchEvent(new Event('error'));
        fixture.detectChanges();
        expect(image().getAttribute('src')).toBe('assets/images/stubs/meals/lunch.svg');
        fixture.componentRef.setInput('meal', { ...meal, imageUrl: 'https://example.com/new.jpg' });
        fixture.detectChanges();
        expect(image().getAttribute('src')).toBe('https://example.com/new.jpg');
        fixture.componentRef.setInput('meal', { ...meal, imageUrl: null, mealType: null, itemNames: [] });
        fixture.detectChanges();
        expect(image().getAttribute('src')).toBe('assets/images/stubs/meals/other.svg');
        expect((fixture.nativeElement as HTMLElement).querySelector('.favorite-row__composition')).toBeNull();
    });
    it('uses a bounded distinct product collage when the meal has no own photo', () => {
        const urls = ['a.jpg', 'b.jpg', 'c.jpg', 'd.jpg'];
        const fixture = create({ ...meal, imageUrl: null, itemImageUrls: [...urls, 'a.jpg', 'extra.jpg'] });
        const images = (): HTMLImageElement[] => [...(fixture.nativeElement as HTMLElement).querySelectorAll('img')];
        expect(images().map(image => image.getAttribute('src'))).toEqual(urls);
        images()[0].dispatchEvent(new Event('error'));
        fixture.detectChanges();
        expect(images().map(image => image.getAttribute('src'))).not.toContain('a.jpg');
    });
    it('emits removal independently of repeating the meal', () => {
        const fixture = create();
        const removed = vi.fn();
        const added = vi.fn();
        fixture.componentInstance.remove.subscribe(removed);
        fixture.componentInstance.add.subscribe(added);
        fixture.debugElement.query(By.css('.favorite-row__star')).triggerEventHandler('click');
        expect(removed).toHaveBeenCalledWith(meal);
        expect(added).not.toHaveBeenCalled();
    });
    it('emits the selected favorite and disables its action while another addition is pending', () => {
        const fixture = create();
        const selected = vi.fn();
        fixture.componentInstance.add.subscribe(selected);
        const button = fixture.debugElement
            .queryAll(By.directive(FdUiButtonComponent))
            .find(item => (item.componentInstance as FdUiButtonComponent).icon() === 'add');
        if (button === undefined) {
            throw new Error('Add action missing');
        }
        button.triggerEventHandler('click');
        expect(selected).toHaveBeenCalledWith(meal);
        fixture.componentRef.setInput('disabled', true);
        fixture.detectChanges();
        expect((button.componentInstance as FdUiButtonComponent).disabled()).toBe(true);
    });
});
