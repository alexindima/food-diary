import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { FdUiSelectComponent } from 'fd-ui-kit';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { RECIPE_CATEGORIES } from '../../models/recipe-category';
import { PublicCategoryFilterComponent } from './public-category-filter';

describe('PublicCategoryFilterComponent', () => {
    it('offers every fixed category and emits its code rather than a translated label', () => {
        TestBed.configureTestingModule({ providers: [provideTranslateTesting()] });
        const fixture = TestBed.createComponent(PublicCategoryFilterComponent);
        fixture.detectChanges();
        const changed = vi.fn();
        fixture.componentInstance.category.subscribe(changed);
        const select = fixture.debugElement.query(By.directive(FdUiSelectComponent)).componentInstance as FdUiSelectComponent<string>;
        expect(select.options().map(option => option.value)).toEqual(['', ...RECIPE_CATEGORIES]);
        expect(select.options()[1].label).toBe('RECIPE_CATEGORIES.other');
        select.value.set('salads');
        expect(changed).toHaveBeenCalledWith('salads');
        select.value.set('');
        expect(changed).toHaveBeenLastCalledWith('');
    });
});
