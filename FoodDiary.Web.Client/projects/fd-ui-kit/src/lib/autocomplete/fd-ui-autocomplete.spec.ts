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
});
