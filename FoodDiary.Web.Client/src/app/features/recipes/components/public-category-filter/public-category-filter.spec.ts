import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { FdUiAutocompleteComponent } from 'fd-ui-kit';
import { of, throwError } from 'rxjs';
import { describe, expect, it, type Mock, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { PublicRecipesFacade } from '../../lib/public-recipes.facade';
import { PublicCategoryFilterComponent } from './public-category-filter';

describe('PublicCategoryFilterComponent', () => {
    function setup(): {
        fixture: ComponentFixture<PublicCategoryFilterComponent>;
        facade: { getCategories: Mock };
        control: FdUiAutocompleteComponent<{ name: string; label: string }>;
    } {
        const facade = { getCategories: vi.fn().mockReturnValue(of(['Soups'])) };
        TestBed.configureTestingModule({ providers: [provideTranslateTesting(), { provide: PublicRecipesFacade, useValue: facade }] });
        const fixture = TestBed.createComponent(PublicCategoryFilterComponent);
        fixture.componentRef.setInput('language', 'en');
        fixture.detectChanges();
        const control = fixture.debugElement.query(By.directive(FdUiAutocompleteComponent)).componentInstance as FdUiAutocompleteComponent<{
            name: string;
            label: string;
        }>;
        return { fixture, facade, control };
    }

    it('loads categories for the selected language and applies only a selected option', async () => {
        const { fixture, facade, control } = setup();
        const changed = vi.fn();
        fixture.componentInstance.category.subscribe(changed);
        await vi.waitFor(() => {
            expect(facade.getCategories).toHaveBeenCalledWith('', 'en');
        });
        fixture.detectChanges();
        expect(control.options().map(option => option.value.name)).toEqual(['', 'Soups']);
        expect(control.showClear()).toBe(false);
        control.queryChange.emit('So');
        control.value.set('So');
        fixture.detectChanges();
        expect(changed).not.toHaveBeenCalled();
        await vi.waitFor(() => {
            expect(facade.getCategories).toHaveBeenCalledWith('So', 'en');
        });
        control.optionSelected.emit({ label: 'Soups', value: { name: 'Soups', label: 'Soups' } });
        expect(changed).toHaveBeenLastCalledWith('Soups');
        control.value.set(null);
        expect(changed).toHaveBeenLastCalledWith('');
    });

    it('keeps the all-categories option after an API failure', async () => {
        const { fixture, facade, control } = setup();
        facade.getCategories.mockReturnValue(throwError(() => new Error('offline')));
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(control.error()).toBe('PUBLIC_RECIPES.CATEGORIES_ERROR');
        });
        expect(control.loading()).toBe(false);
        expect(control.options().map(option => option.value.name)).toEqual(['']);
    });
});
