import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import type { DietType } from '../../../../models/meal-plan.data';
import { MealPlanListFiltersComponent } from './meal-plan-list-filters.component';

describe('MealPlanListFiltersComponent', () => {
    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [MealPlanListFiltersComponent, TranslateModule.forRoot()],
        });
    });

    it('builds options from selected type', () => {
        const fixture = createComponent('Keto');
        const component = fixture.componentInstance;

        expect(component.options().find(option => option.value === null)?.fill).toBe('outline');
        expect(component.options().find(option => option.value === 'Keto')?.fill).toBe('solid');
        expect(component.options().find(option => option.value === 'Balanced')?.fill).toBe('outline');
    });

    it('emits selected filter value', () => {
        const fixture = createComponent(null);
        const component = fixture.componentInstance;
        const filterChange = vi.fn();
        component.filterChange.subscribe(filterChange);

        component.filterChange.emit('Balanced');

        expect(filterChange).toHaveBeenCalledWith('Balanced');
    });
});

function createComponent(selectedType: DietType | null): ComponentFixture<MealPlanListFiltersComponent> {
    const fixture = TestBed.createComponent(MealPlanListFiltersComponent);
    fixture.componentRef.setInput('selectedType', selectedType);
    fixture.detectChanges();

    return fixture;
}
