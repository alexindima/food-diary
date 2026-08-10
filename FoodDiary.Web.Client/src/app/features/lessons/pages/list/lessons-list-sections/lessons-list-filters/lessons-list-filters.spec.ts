import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../testing/translate-testing.module';
import { LessonsListFiltersComponent } from './lessons-list-filters';

describe('LessonsListFiltersComponent', () => {
    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [LessonsListFiltersComponent],
            providers: [provideTranslateTesting()],
        });
    });

    it('builds options from selected category', () => {
        const fixture = createComponent(['Hydration', 'Macronutrients'], 'Hydration');
        const component = fixture.componentInstance;

        expect(component['options']().find(option => option.value === null)?.fill).toBe('outline');
        expect(component['options']().find(option => option.value === 'Hydration')?.fill).toBe('solid');
    });

    it('renders only available categories and exposes the selected button', () => {
        const categories = ['Hydration', 'Macronutrients'];
        const fixture = createComponent(categories, 'Hydration');
        const element = fixture.nativeElement as HTMLElement;
        const buttons = Array.from(element.querySelectorAll('button'));

        expect(buttons).toHaveLength(categories.length + 1);
        expect(buttons.filter(button => button.getAttribute('aria-pressed') === 'true')).toHaveLength(1);
        expect(buttons[1].getAttribute('aria-pressed')).toBe('true');
        expect(buttons.filter(button => button.classList.contains('fd-ui-button--solid'))).toHaveLength(1);
        expect(buttons[1].classList).toContain('fd-ui-button--solid');
        expect(buttons[0].classList).toContain('fd-ui-button--secondary');
        expect(buttons[1].classList).toContain('fd-ui-button--primary');
    });

    it('emits selected category', () => {
        const fixture = createComponent(['Macronutrients'], null);
        const filterChange = vi.fn();
        fixture.componentInstance['filterChange'].subscribe(filterChange);

        fixture.componentInstance['filterChange'].emit('Macronutrients');

        expect(filterChange).toHaveBeenCalledWith('Macronutrients');
    });
});

function createComponent(categories: readonly string[], selectedCategory: string | null): ComponentFixture<LessonsListFiltersComponent> {
    const fixture = TestBed.createComponent(LessonsListFiltersComponent);
    fixture.componentRef.setInput('categories', categories);
    fixture.componentRef.setInput('selectedCategory', selectedCategory);
    fixture.detectChanges();

    return fixture;
}
