import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { LessonsListFiltersComponent } from './lessons-list-filters.component';

describe('LessonsListFiltersComponent', () => {
    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [LessonsListFiltersComponent, TranslateModule.forRoot()],
        });
    });

    it('builds options from selected category', () => {
        const fixture = createComponent('Hydration');
        const component = fixture.componentInstance;

        expect(component.options().find(option => option.value === null)?.fill).toBe('outline');
        expect(component.options().find(option => option.value === 'Hydration')?.fill).toBe('solid');
    });

    it('emits selected category', () => {
        const fixture = createComponent(null);
        const filterChange = vi.fn();
        fixture.componentInstance.filterChange.subscribe(filterChange);

        fixture.componentInstance.filterChange.emit('Macronutrients');

        expect(filterChange).toHaveBeenCalledWith('Macronutrients');
    });
});

function createComponent(selectedCategory: string | null): ComponentFixture<LessonsListFiltersComponent> {
    const fixture = TestBed.createComponent(LessonsListFiltersComponent);
    fixture.componentRef.setInput('selectedCategory', selectedCategory);
    fixture.detectChanges();

    return fixture;
}
