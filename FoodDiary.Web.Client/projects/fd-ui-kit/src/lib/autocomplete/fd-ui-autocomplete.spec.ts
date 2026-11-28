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
    it.each([{ isComposing: true }, { keyCode: 229 }])('leaves IME confirmation to the text input: %j', composition => {
        const fixture = TestBed.createComponent(FdUiAutocompleteComponent);
        fixture.componentRef.setInput('options', [{ value: 'apple', label: 'Apple' }]);
        fixture.detectChanges();
        const input = (fixture.nativeElement as HTMLElement).querySelector('input');
        input?.focus();
        fixture.detectChanges();
        const selected = vi.fn();
        fixture.componentInstance.optionSelected.subscribe(selected);
        const event = new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, cancelable: true, ...composition });
        input?.dispatchEvent(event);
        fixture.detectChanges();
        expect(selected).not.toHaveBeenCalled();
        expect(event.defaultPrevented).toBe(false);
        input?.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, cancelable: true }));
        fixture.detectChanges();
        expect(selected).toHaveBeenCalledOnce();
    });

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

describe('FdUiAutocompleteComponent query editing', () => {
    it('replaces a selected object with the typed query and clears it through the suffix button', () => {
        const fixture = TestBed.createComponent(FdUiAutocompleteComponent);
        const apple = { id: 'apple' };
        fixture.componentRef.setInput('options', [{ value: apple, label: 'Apple' }]);
        fixture.componentRef.setInput('value', apple);
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        const input = host.querySelector('input');
        if (input === null) {
            throw new Error('Expected the autocomplete input.');
        }
        const queryChanged = vi.fn();
        const valueChanged = vi.fn();
        fixture.componentInstance.queryChange.subscribe(queryChanged);
        fixture.componentInstance.value.subscribe(valueChanged);

        expect(input.value).toBe('Apple');
        input.focus();
        input.value = 'Pear';
        input.dispatchEvent(new Event('input', { bubbles: true }));
        fixture.detectChanges();

        expect(fixture.componentInstance.value()).toBe('Pear');
        expect(queryChanged).toHaveBeenCalledExactlyOnceWith('Pear');
        expect(document.querySelector('[role="option"]')?.getAttribute('aria-selected')).toBe('false');

        host.querySelector<HTMLButtonElement>('button[aria-label="Clear"]')?.click();
        fixture.detectChanges();

        expect(input.value).toBe('');
        expect(valueChanged.mock.calls).toEqual([['Pear'], ['']]);
        expect(queryChanged.mock.calls).toEqual([['Pear'], ['']]);
        expect(document.activeElement).toBe(input);
        expect(input.getAttribute('aria-expanded')).toBe('false');
        expect(document.querySelector('[role="listbox"]')).toBeNull();
    });

    it('shows a loading state followed by no results and lets Escape dismiss it without changing the query', () => {
        const fixture = TestBed.createComponent(FdUiAutocompleteComponent);
        fixture.componentRef.setInput('emptyText', 'No matches');
        fixture.detectChanges();
        const input = (fixture.nativeElement as HTMLElement).querySelector('input');
        if (input === null) {
            throw new Error('Expected the autocomplete input.');
        }
        const writeQuery = (query: string): void => {
            input.value = query;
            input.dispatchEvent(new Event('input', { bubbles: true }));
            fixture.detectChanges();
        };

        writeQuery('x');
        expect(document.querySelector('[role="listbox"]')).toBeNull();
        fixture.componentRef.setInput('loading', true);
        writeQuery('xy');
        expect(document.querySelector('[role="listbox"] fd-ui-loader')).not.toBeNull();

        fixture.componentRef.setInput('loading', false);
        fixture.detectChanges();
        expect(document.querySelector('[role="listbox"]')?.textContent).toContain('No matches');
        const escape = new KeyboardEvent('keydown', { key: 'Escape', bubbles: true, cancelable: true });
        input.dispatchEvent(escape);
        fixture.detectChanges();

        expect(escape.defaultPrevented).toBe(true);
        expect(document.querySelector('[role="listbox"]')).toBeNull();
        expect(fixture.componentInstance.value()).toBe('xy');
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

    it.each([
        { model: 'previous', text: 'stored-key', selected: 'false', events: ['value', 'touched', 'touch', 'option'] },
        { model: 'stored-key', text: 'Display label', selected: 'true', events: ['touched', 'touch', 'option'] },
    ])('keeps the string option label contract when the model is $model', ({ model, text, selected, events }) => {
        const fixture = TestBed.createComponent(FdUiAutocompleteComponent);
        const component = fixture.componentInstance;
        const option = { value: 'stored-key', label: 'Display label' };
        const published: string[] = [];
        fixture.componentRef.setInput('options', [option]);
        fixture.componentRef.setInput('value', model);
        fixture.detectChanges();
        const control = requireAutocompleteInput(fixture);
        expect(control.value).toBe(model);

        component.value.subscribe(() => published.push('value'));
        component.touched.subscribe(() => published.push('touched'));
        component.touch.subscribe(() => published.push('touch'));
        component.optionSelected.subscribe(() => published.push('option'));
        control.focus();
        fixture.detectChanges();
        control.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));
        fixture.detectChanges();

        expect(component.value()).toBe(option.value);
        expect(control.value).toBe(text);
        expect(published).toEqual(events);
        expect(document.activeElement).toBe(control);
        expect(control.getAttribute('aria-expanded')).toBe('false');

        control.blur();
        control.focus();
        fixture.detectChanges();
        expect(document.querySelector('[role="option"]')?.getAttribute('aria-selected')).toBe(selected);
    });

    it('uses the displayed selection when focus precedes a replacement model render', () => {
        const fixture = TestBed.createComponent(FdUiAutocompleteComponent);
        const apple = { id: 'apple' };
        const pear = { id: 'pear' };
        fixture.componentRef.setInput('options', [
            { value: apple, label: 'Apple' },
            { value: pear, label: 'Pear' },
        ]);
        fixture.componentRef.setInput('value', pear);
        fixture.detectChanges();
        const control = requireAutocompleteInput(fixture);
        expect(control.value).toBe('Pear');

        fixture.componentRef.setInput('value', apple);
        control.focus();
        fixture.detectChanges();

        expect(control.value).toBe('Apple');
        expect(control.getAttribute('aria-activedescendant')).toBe(`${control.id}-option-1`);
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

    it('lets Enter pass until a programmatic query renders its no-results popup', () => {
        const fixture = TestBed.createComponent(FdUiAutocompleteComponent);
        fixture.componentRef.setInput('emptyText', 'No matches');
        fixture.componentRef.setInput('value', 'x');
        fixture.detectChanges();
        const control = requireAutocompleteInput(fixture);
        control.focus();
        fixture.detectChanges();

        fixture.componentRef.setInput('value', 'Longer query');
        const earlyEnter = new KeyboardEvent('keydown', { key: 'Enter', cancelable: true });
        control.dispatchEvent(earlyEnter);
        expect(earlyEnter.defaultPrevented).toBe(false);

        fixture.detectChanges();
        const renderedEnter = new KeyboardEvent('keydown', { key: 'Enter', cancelable: true });
        control.dispatchEvent(renderedEnter);
        expect(renderedEnter.defaultPrevented).toBe(true);
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
        const earlyEnter = new KeyboardEvent('keydown', { key: 'Enter', cancelable: true });
        component['onControlKeydown'](earlyEnter);
        expect(earlyEnter.defaultPrevented).toBe(true);
        expect(selected).not.toHaveBeenCalled();

        fixture.detectChanges();
        component['onControlKeydown'](new KeyboardEvent('keydown', { key: 'Enter' }));
        fixture.detectChanges();

        expect(selected).toHaveBeenCalledExactlyOnceWith(option);
        expect(component.value()).toBe('Paris');
        expect((fixture.nativeElement as HTMLElement).querySelector('input')?.getAttribute('aria-expanded')).toBe('false');
    });
});

describe('Autocomplete external values and errors', () => {
    it('renders the consumer-provided display text for a selection outside the current results', () => {
        const fixture = TestBed.createComponent(FdUiAutocompleteComponent);
        const saved = { code: 'C25' };
        const displayWith = vi.fn((value: { code: string } | null) => (value === null ? '' : `Category ${value.code}`));
        fixture.componentRef.setInput('displayWith', displayWith);
        fixture.componentRef.setInput('value', saved);
        fixture.detectChanges();
        const input = requireAutocompleteInput(fixture);
        expect(input.value).toBe('Category C25');
        expect(displayWith).toHaveBeenCalledWith(saved);

        fixture.componentRef.setInput('options', [{ value: saved, label: 'Fruit' }]);
        fixture.detectChanges();
        expect(input.value).toBe('Fruit');

        fixture.componentRef.setInput('displayWith', undefined);
        const externalCode = 25;
        fixture.componentRef.setInput('value', externalCode);
        fixture.detectChanges();
        expect(input.value).toBe('25');
    });

    it('links a domain error to the input and removes its accessible invalid state when cleared', () => {
        const fixture = TestBed.createComponent(FdUiAutocompleteComponent);
        fixture.componentRef.setInput('error', 'Choose an available product');
        fixture.detectChanges();
        const root = fixture.nativeElement as HTMLElement;
        const input = requireAutocompleteInput(fixture);
        const error = root.querySelector('[role="alert"]');

        expect(error?.textContent).toContain('Choose an available product');
        expect(input.getAttribute('aria-invalid')).toBe('true');
        expect(input.getAttribute('aria-describedby')).toBe(error?.id);
        expect(root.querySelector('.fd-ui-autocomplete--has-error')).not.toBeNull();

        fixture.componentRef.setInput('error', null);
        fixture.detectChanges();
        expect(input.hasAttribute('aria-invalid')).toBe(false);
        expect(input.hasAttribute('aria-describedby')).toBe(false);
        expect(root.querySelector('[role="alert"]')).toBeNull();
    });
});

describe('Autocomplete disabled and empty interactions', () => {
    it('ignores a pending option selection, clear click and input event after being disabled', () => {
        const fixture = TestBed.createComponent(FdUiAutocompleteComponent);
        fixture.componentRef.setInput('value', 'Search');
        fixture.componentRef.setInput('options', [{ value: 'Apple', label: 'Apple' }]);
        fixture.detectChanges();
        const input = requireAutocompleteInput(fixture);
        const queryChanged = vi.fn();
        const selected = vi.fn();
        const changed = vi.fn();
        fixture.componentInstance.queryChange.subscribe(queryChanged);
        fixture.componentInstance.optionSelected.subscribe(selected);
        fixture.componentInstance.value.subscribe(changed);
        fixture.componentInstance.focus();
        fixture.detectChanges();
        const option = document.querySelector<HTMLButtonElement>('[role="option"]');
        const clear = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('button[aria-label="Clear"]');
        expect(document.activeElement).toBe(input);
        expect(option).not.toBeNull();

        fixture.componentRef.setInput('disabled', true);
        fixture.detectChanges();
        option?.click();
        clear?.dispatchEvent(new MouseEvent('click', { bubbles: true }));
        input.dispatchEvent(new Event('input', { bubbles: true }));
        fixture.detectChanges();

        expect(input.disabled).toBe(true);
        expect(clear?.disabled).toBe(true);
        expect(selected).not.toHaveBeenCalled();
        expect(queryChanged).not.toHaveBeenCalled();
        expect(changed).not.toHaveBeenCalled();
        expect(fixture.componentInstance.value()).toBe('Search');
    });

    it('suppresses Escape only when open and leaves no active option when navigating empty results', () => {
        const fixture = TestBed.createComponent(FdUiAutocompleteComponent);
        fixture.componentRef.setInput('value', 'Peaches');
        fixture.componentRef.setInput('emptyText', 'No products');
        fixture.detectChanges();
        const input = requireAutocompleteInput(fixture);
        const closedEscape = new KeyboardEvent('keydown', { key: 'Escape', bubbles: true, cancelable: true });
        input.dispatchEvent(closedEscape);
        expect(closedEscape.defaultPrevented).toBe(false);

        input.focus();
        fixture.detectChanges();
        const arrowDown = new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true, cancelable: true });
        input.dispatchEvent(arrowDown);
        fixture.detectChanges();
        expect(arrowDown.defaultPrevented).toBe(true);
        expect(input.hasAttribute('aria-activedescendant')).toBe(false);
        expect(document.querySelector('[role="listbox"]')?.textContent).toContain('No products');
    });
});

function requireAutocompleteInput(fixture: ComponentFixture<FdUiAutocompleteComponent>): HTMLInputElement {
    const input = (fixture.nativeElement as HTMLElement).querySelector('input');
    if (input === null) {
        throw new Error('Expected the autocomplete input.');
    }
    return input;
}
