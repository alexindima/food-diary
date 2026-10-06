import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { FdUiAutocompleteComponent } from './fd-ui-autocomplete';

const originalScrollIntoView = Object.getOwnPropertyDescriptor(Element.prototype, 'scrollIntoView');
const scrollIntoView = vi.fn<Element['scrollIntoView']>();
beforeEach(() => {
    scrollIntoView.mockClear();
    Object.defineProperty(Element.prototype, 'scrollIntoView', { configurable: true, value: scrollIntoView });
});
afterEach(() => {
    if (originalScrollIntoView === undefined) {
        Reflect.deleteProperty(Element.prototype, 'scrollIntoView');
    } else {
        Object.defineProperty(Element.prototype, 'scrollIntoView', originalScrollIntoView);
    }
});

describe('FdUiAutocompleteComponent', () => {
    it('keeps late suggestions closed after focus moves to another field', () => {
        const fixture = TestBed.createComponent(FdUiAutocompleteComponent);
        fixture.componentRef.setInput('value', 'Apple');
        fixture.detectChanges();
        const input = (fixture.nativeElement as HTMLElement).querySelector('input');
        input?.dispatchEvent(new FocusEvent('focus'));
        fixture.detectChanges();
        input?.dispatchEvent(new FocusEvent('blur', { relatedTarget: document.createElement('input') }));
        fixture.componentRef.setInput('options', [{ value: 'Apple', label: 'Apple' }]);
        fixture.detectChanges();

        expect(input?.getAttribute('aria-expanded')).toBe('false');
        expect(document.querySelector('[role="listbox"]')).toBeNull();
        expect(fixture.componentInstance.touched()).toBe(true);
    });

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

describe('Autocomplete saved selection', () => {
    it('activates and scrolls the selected value on focus so Enter preserves it', async () => {
        const fixture = TestBed.createComponent(FdUiAutocompleteComponent);
        fixture.componentRef.setInput(
            'options',
            ['Alpha', 'Bravo', 'Charlie', 'Delta'].map(label => ({ value: label, label })),
        );
        fixture.componentRef.setInput('value', 'Charlie');
        fixture.detectChanges();
        const input = (fixture.nativeElement as HTMLElement).querySelector('input');
        input?.focus();
        fixture.detectChanges();
        await fixture.whenStable();
        const selected = Array.from(document.querySelectorAll<HTMLElement>('[role="option"]')).find(option =>
            option.textContent.includes('Charlie'),
        );

        expect(input?.getAttribute('aria-activedescendant')).toBe(selected?.id);
        expect(scrollIntoView.mock.contexts).toContain(selected);
        input?.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));
        fixture.detectChanges();

        expect(fixture.componentInstance.value()).toBe('Charlie');
        expect(input?.value).toBe('Charlie');
        expect(input?.getAttribute('aria-expanded')).toBe('false');
    });
});

describe('Autocomplete multi-option keyboard navigation', () => {
    it.each([
        ['ArrowDown', 'Delta'],
        ['ArrowUp', 'Bravo'],
    ])('continues moving through options with repeated %s keys', (key, expected) => {
        const fixture = TestBed.createComponent(FdUiAutocompleteComponent);
        fixture.componentRef.setInput(
            'options',
            ['Alpha', 'Bravo', 'Charlie', 'Delta'].map(label => ({ value: label, label })),
        );
        fixture.detectChanges();
        const component = fixture.componentInstance;
        const selected = vi.fn();
        component.optionSelected.subscribe(selected);
        component['onFocus']();
        fixture.detectChanges();
        const listbox = document.querySelector<HTMLElement>('[role="listbox"]');
        if (listbox !== null) {
            listbox.scrollTo = vi.fn();
        }

        for (const navigationKey of [key, key, key]) {
            component['onControlKeydown'](new KeyboardEvent('keydown', { key: navigationKey }));
            fixture.detectChanges();
        }
        component['onControlKeydown'](new KeyboardEvent('keydown', { key: 'Enter' }));
        fixture.detectChanges();

        expect(component.value()).toBe(expected);
        expect(selected).toHaveBeenCalledExactlyOnceWith({ value: expected, label: expected });
    });
});

describe('Autocomplete keyboard scrolling', () => {
    it('keeps the last option active and scrolls it into view when ArrowUp opens a closed menu', async () => {
        const fixture = TestBed.createComponent(FdUiAutocompleteComponent);
        fixture.componentRef.setInput(
            'options',
            ['Alpha', 'Bravo', 'Charlie', 'Delta'].map(label => ({ value: label, label })),
        );
        fixture.detectChanges();
        const component = fixture.componentInstance;

        component['onControlKeydown'](new KeyboardEvent('keydown', { key: 'ArrowUp' }));
        fixture.detectChanges();
        await fixture.whenStable();
        fixture.detectChanges();

        const host = fixture.nativeElement as HTMLElement;
        const options = Array.from(document.querySelectorAll<HTMLElement>('[role="option"]'));
        const last = options.at(-1);
        expect(host.querySelector('input')?.getAttribute('aria-activedescendant')).toBe(last?.id);
        expect(last?.textContent).toContain('Delta');
        expect(scrollIntoView).toHaveBeenCalledWith({ block: 'nearest', inline: 'nearest' });
        expect(scrollIntoView.mock.contexts).toContain(last);
    });

    it('scrolls a newly active first option after filtering an already open menu', async () => {
        const fixture = TestBed.createComponent(FdUiAutocompleteComponent);
        fixture.componentRef.setInput(
            'options',
            ['Alpha', 'Bravo', 'Charlie', 'Delta'].map(label => ({ value: label, label })),
        );
        fixture.detectChanges();
        const component = fixture.componentInstance;
        component['onControlKeydown'](new KeyboardEvent('keydown', { key: 'ArrowUp' }));
        fixture.detectChanges();
        await fixture.whenStable();

        fixture.componentRef.setInput('options', [{ value: 'Bravo', label: 'Bravo' }]);
        fixture.detectChanges();
        await fixture.whenStable();
        fixture.detectChanges();

        const option = document.querySelector<HTMLElement>('[role="option"]');
        expect((fixture.nativeElement as HTMLElement).querySelector('input')?.getAttribute('aria-activedescendant')).toBe(option?.id);
        expect(option?.textContent).toContain('Bravo');
        expect(scrollIntoView).toHaveBeenCalledWith({ block: 'nearest', inline: 'nearest' });
        expect(scrollIntoView.mock.contexts).toContain(option);
    });
});

describe('Autocomplete changing suggestions', () => {
    it('ignores Enter safely while no suggestions match and prevents submitting the parent form', () => {
        const fixture = TestBed.createComponent(FdUiAutocompleteComponent);
        fixture.componentRef.setInput('value', 'No matches');
        fixture.componentRef.setInput('emptyText', 'No matching options');
        fixture.componentRef.setInput('options', [{ value: 'Paris', label: 'Paris' }]);
        fixture.detectChanges();
        const component = fixture.componentInstance;
        const selected = vi.fn();
        component.optionSelected.subscribe(selected);
        component['onFocus']();
        fixture.componentRef.setInput('options', []);
        fixture.detectChanges();
        const event = new KeyboardEvent('keydown', { key: 'Enter', cancelable: true });

        expect(() => {
            component['onControlKeydown'](event);
        }).not.toThrow();
        expect(event.defaultPrevented).toBe(true);
        expect(selected).not.toHaveBeenCalled();
        expect(component.value()).toBe('No matches');
    });

    it('allows Enter to submit a free-text form when the empty suggestion popup is hidden', () => {
        const fixture = TestBed.createComponent(FdUiAutocompleteComponent);
        fixture.componentRef.setInput('showEmptyState', false);
        fixture.componentRef.setInput('value', 'New product');
        fixture.detectChanges();
        const component = fixture.componentInstance;
        const selected = vi.fn();
        component.optionSelected.subscribe(selected);
        component['onFocus']();
        fixture.detectChanges();
        const event = new KeyboardEvent('keydown', { key: 'Enter', cancelable: true });

        component['onControlKeydown'](event);

        expect(event.defaultPrevented).toBe(false);
        expect(selected).not.toHaveBeenCalled();
        expect(component.value()).toBe('New product');
        expect(document.querySelector('[role="listbox"]')).toBeNull();
    });

    it('activates a returned suggestion for keyboard selection after a search without results', () => {
        const fixture = TestBed.createComponent(FdUiAutocompleteComponent);
        fixture.detectChanges();
        const component = fixture.componentInstance;
        const option = { value: 'Paris', label: 'Paris' };
        const selected = vi.fn();
        component.optionSelected.subscribe(selected);
        component['onFocus']();
        fixture.detectChanges();
        fixture.componentRef.setInput('options', [option]);
        fixture.detectChanges();
        component['onControlKeydown'](new KeyboardEvent('keydown', { key: 'Enter' }));
        fixture.detectChanges();

        expect(selected).toHaveBeenCalledExactlyOnceWith(option);
        expect(component.value()).toBe('Paris');
        expect((fixture.nativeElement as HTMLElement).querySelector('input')?.getAttribute('aria-expanded')).toBe('false');
    });
});
