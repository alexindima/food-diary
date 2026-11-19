import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { describe, expect, it, vi } from 'vitest';

import { FdUiRadioGroupComponent, type FdUiRadioOption } from './fd-ui-radio-group';

const OPTION_COUNT = 3;
const TEST_OPTIONS: Array<FdUiRadioOption<string>> = [
    { value: 'a', label: 'Option A' },
    { value: 'b', label: 'Option B', description: 'Description B' },
    { value: 'c', label: 'Option C' },
];

type RadioGroupTestContext = {
    component: FdUiRadioGroupComponent<string>;
    fixture: ComponentFixture<FdUiRadioGroupComponent<string>>;
    host: () => HTMLElement;
    requireElement: (selector: string) => HTMLElement;
};

async function setupRadioGroupAsync(): Promise<RadioGroupTestContext> {
    await TestBed.configureTestingModule({
        imports: [FdUiRadioGroupComponent],
    }).compileComponents();

    const fixture = TestBed.createComponent(FdUiRadioGroupComponent<string>);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    const host = (): HTMLElement => fixture.nativeElement as HTMLElement;
    const requireElement = (selector: string): HTMLElement => {
        const element = host().querySelector<HTMLElement>(selector);
        if (element === null) {
            throw new Error(`Expected element ${selector} to exist.`);
        }

        return element;
    };

    return { component, fixture, host, requireElement };
}

function setRadioOptions(fixture: ComponentFixture<FdUiRadioGroupComponent<string>>): void {
    fixture.componentRef.setInput('options', TEST_OPTIONS);
    fixture.detectChanges();
}

describe('FdUiRadioGroupComponent', () => {
    it('should create', async () => {
        const { component } = await setupRadioGroupAsync();

        expect(component).toBeTruthy();
    });

    it('should render label', async () => {
        const { fixture, requireElement } = await setupRadioGroupAsync();

        fixture.componentRef.setInput('label', 'Choose one');
        fixture.detectChanges();

        const label = requireElement('.fd-ui-radio-group__label');
        expect(label.textContent).toContain('Choose one');
        expect(requireElement('[role="radiogroup"]').getAttribute('aria-labelledby')).toBe(label.id);
    });

    it('should render options', async () => {
        const { fixture, host } = await setupRadioGroupAsync();

        setRadioOptions(fixture);
        const radioButtons = Array.from(host().querySelectorAll<HTMLElement>('.fd-ui-radio'));
        expect(radioButtons.length).toBe(OPTION_COUNT);
        expect(radioButtons[0].textContent).toContain('Option A');
        expect(radioButtons[1].textContent).toContain('Option B');
        expect(radioButtons[2].textContent).toContain('Option C');
    });
});

describe('FdUiRadioGroupComponent signal form control', () => {
    it('should write value from model', async () => {
        const { component, fixture } = await setupRadioGroupAsync();

        setRadioOptions(fixture);
        component.value.set('b');
        fixture.detectChanges();

        expect(component.value()).toBe('b');
    });

    it('should update value on selection', async () => {
        const { component, fixture } = await setupRadioGroupAsync();

        setRadioOptions(fixture);

        component['selectOption'](TEST_OPTIONS[0]);
        fixture.detectChanges();

        expect(component.value()).toBe('a');
    });

    it('moves focus with the selected option when using arrow keys', async () => {
        const { component, fixture, requireElement } = await setupRadioGroupAsync();
        setRadioOptions(fixture);

        const first = requireElement('input[type="radio"]:first-of-type');
        const second = requireElement('input[type="radio"]:nth-of-type(2)');
        first.focus();
        first.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true, cancelable: true }));
        fixture.detectChanges();

        expect(component.value()).toBe('b');
        expect(second.ownerDocument.activeElement).toBe(second);
    });

    it('should mark touched on blur', async () => {
        const { component, fixture } = await setupRadioGroupAsync();

        setRadioOptions(fixture);

        const radioInput = fixture.debugElement.query(By.css('.fd-ui-radio__input'));
        radioInput.triggerEventHandler('blur', {});
        fixture.detectChanges();

        expect(component.touched()).toBe(true);
    });

    it('should set disabled state', async () => {
        const { component, fixture } = await setupRadioGroupAsync();

        setRadioOptions(fixture);
        fixture.componentRef.setInput('disabled', true);
        fixture.detectChanges();

        expect(component['disabled']()).toBe(true);

        fixture.componentRef.setInput('disabled', false);
        fixture.detectChanges();

        expect(component['disabled']()).toBe(false);
    });
});

describe('FdUiRadioGroupComponent helper text', () => {
    it('should display error message', async () => {
        const { fixture, requireElement } = await setupRadioGroupAsync();

        fixture.componentRef.setInput('error', 'This field is required');
        fixture.detectChanges();

        const error = requireElement('.fd-ui-radio-group__error');
        expect(error.textContent).toContain('This field is required');
    });

    it('should display hint text', async () => {
        const { fixture, requireElement } = await setupRadioGroupAsync();

        fixture.componentRef.setInput('hint', 'Select your preference');
        fixture.detectChanges();

        const hint = requireElement('.fd-ui-radio-group__hint');
        expect(hint.textContent).toContain('Select your preference');
    });
});

describe('FdUiRadioGroupComponent native keyboard behavior', () => {
    it.each([
        { key: 'ArrowRight', start: 'c', target: 'a' },
        { key: 'ArrowLeft', start: 'a', target: 'c' },
        { key: 'ArrowUp', start: 'a', target: 'c' },
        { key: 'Home', start: 'b', target: 'a' },
        { key: 'End', start: 'b', target: 'c' },
    ])('uses $key to select $target and move focus from $start', async ({ key, start, target }) => {
        const { component, fixture, host } = await setupRadioGroupAsync();
        setRadioOptions(fixture);
        component.value.set(start);
        fixture.detectChanges();
        const focused = host().querySelector<HTMLInputElement>('input:checked');
        const changed = vi.fn();
        component.value.subscribe(changed);

        focused?.focus();
        const event = new KeyboardEvent('keydown', { key, bubbles: true, cancelable: true });
        focused?.dispatchEvent(event);
        fixture.detectChanges();

        expect(event.defaultPrevented).toBe(true);
        expect(changed).toHaveBeenCalledExactlyOnceWith(target);
        expect(document.activeElement).toBe(host().querySelector('input:checked'));
        expect(document.activeElement).not.toBe(focused);
    });

    it('lets Tab leave the radio group without selecting a different option', async () => {
        const { component, fixture, requireElement } = await setupRadioGroupAsync();
        setRadioOptions(fixture);
        component.value.set('b');
        fixture.detectChanges();
        const selected = requireElement('input:checked');
        selected.focus();
        const tab = new KeyboardEvent('keydown', { key: 'Tab', bubbles: true, cancelable: true });
        selected.dispatchEvent(tab);
        fixture.detectChanges();

        expect(tab.defaultPrevented).toBe(false);
        expect(component.value()).toBe('b');
    });
});

describe('FdUiRadioGroupComponent stale native events', () => {
    it('does not select stale options after the group is cleared but before the native controls repaint', async () => {
        const { component, fixture, host, requireElement } = await setupRadioGroupAsync();
        setRadioOptions(fixture);
        const first = requireElement('input[type="radio"]');
        const changed = vi.fn();
        component.value.subscribe(changed);

        fixture.componentRef.setInput('options', []);
        const event = new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true, cancelable: true });
        first.dispatchEvent(event);
        fixture.detectChanges();

        expect(event.defaultPrevented).toBe(false);
        expect(host().querySelector('input[type="radio"]')).toBeNull();
        expect(component.value()).toBeNull();
        expect(changed).not.toHaveBeenCalled();
    });

    it('blocks a pending native selection after the host disables the group', async () => {
        const { component, fixture, requireElement } = await setupRadioGroupAsync();
        setRadioOptions(fixture);
        const first = requireElement('input[type="radio"]');
        const changed = vi.fn();
        component.value.subscribe(changed);

        fixture.componentRef.setInput('disabled', true);
        first.click();
        fixture.detectChanges();

        expect(component.value()).toBeNull();
        expect(changed).not.toHaveBeenCalled();
        expect(requireElement('input[type="radio"]').hasAttribute('disabled')).toBe(true);
    });
});
