import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { FdUiAutocompleteComponent } from './fd-ui-autocomplete';

describe('FdUiAutocompleteComponent', () => {
    it('should expose the configured accessible name on the clear button', async () => {
        await TestBed.configureTestingModule({ imports: [FdUiAutocompleteComponent] }).compileComponents();
        const fixture: ComponentFixture<FdUiAutocompleteComponent> = TestBed.createComponent(FdUiAutocompleteComponent);
        fixture.componentRef.setInput('value', 'Apple');
        fixture.componentRef.setInput('clearAriaLabel', 'Очистить');
        fixture.detectChanges();

        const clearButton = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('.fd-ui-autocomplete__suffix--button');

        expect(clearButton?.getAttribute('aria-label')).toBe('Очистить');
    });
    it('shows a dropdown affordance when clearing is disabled', () => {
        const fixture = TestBed.createComponent(FdUiAutocompleteComponent);
        fixture.componentRef.setInput('value', 'Any category');
        fixture.componentRef.setInput('showClear', false);
        fixture.componentRef.setInput('suffixIcon', 'expand_more');
        fixture.detectChanges();
        const element = fixture.nativeElement as HTMLElement;
        expect(element.querySelector('.fd-ui-autocomplete__suffix--button')).toBeNull();
        expect(element.querySelector('.fd-ui-autocomplete__suffix')?.textContent).toContain('expand_more');
    });

    it('closes the menu after restoring input focus on mouse selection', () => {
        const fixture = TestBed.createComponent(FdUiAutocompleteComponent);
        fixture.componentRef.setInput('options', [{ value: 'Soups', label: 'Soups' }]);
        fixture.detectChanges();
        const input = (fixture.nativeElement as HTMLElement).querySelector('input');
        input?.focus();
        fixture.detectChanges();
        const option = document.querySelector<HTMLButtonElement>('[role="option"]');
        option?.focus();
        option?.click();
        fixture.detectChanges();
        expect(document.activeElement).toBe(input);
        expect(input?.getAttribute('aria-expanded')).toBe('false');
        expect(input?.value).toBe('Soups');
    });
});
