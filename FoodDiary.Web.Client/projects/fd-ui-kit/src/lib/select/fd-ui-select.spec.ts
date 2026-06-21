import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { FdUiSelectComponent, type FdUiSelectOption } from './fd-ui-select';

const TEST_OPTIONS: Array<FdUiSelectOption<string>> = [
    { value: 'apple', label: 'Apple' },
    { value: 'banana', label: 'Banana' },
    { value: 'cherry', label: 'Cherry' },
];

type SelectTestContext = {
    component: FdUiSelectComponent<string>;
    fixture: ComponentFixture<FdUiSelectComponent<string>>;
    host: () => HTMLElement;
    requireElement: (selector: string) => HTMLElement;
};

async function setupSelectAsync(): Promise<SelectTestContext> {
    await TestBed.configureTestingModule({
        imports: [FdUiSelectComponent],
        providers: [],
    }).compileComponents();

    const fixture = TestBed.createComponent(FdUiSelectComponent<string>);
    const component = fixture.componentInstance;
    const host = (): HTMLElement => fixture.nativeElement as HTMLElement;
    const requireElement = (selector: string): HTMLElement => {
        const element = host().querySelector<HTMLElement>(selector);
        if (element === null) {
            throw new Error(`Expected element ${selector} to exist.`);
        }

        return element;
    };

    fixture.detectChanges();

    return { component, fixture, host, requireElement };
}

describe('FdUiSelectComponent', () => {
    it('should create', async () => {
        const { component } = await setupSelectAsync();

        expect(component).toBeTruthy();
    });
});

describe('FdUiSelectComponent rendering', () => {
    it('should render label', async () => {
        const { fixture, requireElement } = await setupSelectAsync();
        fixture.componentRef.setInput('label', 'Fruit');
        fixture.detectChanges();

        const labelEl = requireElement('.fd-ui-select__label-text');
        expect(labelEl.textContent.trim()).toBe('Fruit');
    });

    it('should show required asterisk', async () => {
        const { fixture, requireElement } = await setupSelectAsync();
        fixture.componentRef.setInput('label', 'Fruit');
        fixture.componentRef.setInput('required', true);
        fixture.detectChanges();

        const requiredEl = requireElement('.fd-ui-select__required');
        expect(requiredEl.textContent.trim()).toBe('*');
    });

    it('should display error message', async () => {
        const { fixture, requireElement } = await setupSelectAsync();
        fixture.componentRef.setInput('error', 'This field is required');
        fixture.detectChanges();

        const errorEl = requireElement('.fd-ui-select__error');
        expect(errorEl.textContent.trim()).toBe('This field is required');
    });
});

describe('FdUiSelectComponent signal form control', () => {
    it('should write value from model', async () => {
        const { component, fixture } = await setupSelectAsync();
        fixture.componentRef.setInput('options', TEST_OPTIONS);
        fixture.detectChanges();

        component.value.set('banana');
        fixture.detectChanges();

        expect(component['internalValue']()).toBe('banana');
    });

    it('should select option and update value', async () => {
        const { component, fixture } = await setupSelectAsync();
        fixture.componentRef.setInput('options', TEST_OPTIONS);
        fixture.detectChanges();

        component['onOptionSelect'](TEST_OPTIONS[1]);
        fixture.detectChanges();

        expect(component['internalValue']()).toBe('banana');
        expect(component.value()).toBe('banana');
        expect(component.touched()).toBe(true);
    });

    it('should not select when disabled', async () => {
        const { component, fixture } = await setupSelectAsync();
        fixture.componentRef.setInput('options', TEST_OPTIONS);
        fixture.detectChanges();

        fixture.componentRef.setInput('disabled', true);
        fixture.detectChanges();

        component['onOptionSelect'](TEST_OPTIONS[0]);
        fixture.detectChanges();

        expect(component['internalValue']()).toBeNull();
        expect(component.value()).toBeNull();
    });

    it('should set disabled state from input', async () => {
        const { component, fixture } = await setupSelectAsync();
        fixture.componentRef.setInput('disabled', true);
        fixture.detectChanges();

        expect(component['disabled']()).toBe(true);
    });
});

describe('FdUiSelectComponent computed state', () => {
    it('should show selected label', async () => {
        const { component, fixture } = await setupSelectAsync();
        fixture.componentRef.setInput('options', TEST_OPTIONS);
        fixture.detectChanges();

        component.value.set('cherry');
        fixture.detectChanges();

        expect(component['selectedLabel']()).toBe('Cherry');
    });

    it('should float label when has selection', async () => {
        const { component, fixture } = await setupSelectAsync();
        fixture.componentRef.setInput('label', 'Fruit');
        fixture.componentRef.setInput('options', TEST_OPTIONS);
        fixture.detectChanges();

        expect(component['shouldFloatLabel']()).toBe(false);

        component.value.set('apple');
        fixture.detectChanges();

        expect(component['shouldFloatLabel']()).toBe(true);
    });

    it('should expose active option id when menu is open', async () => {
        const { component, fixture } = await setupSelectAsync();
        fixture.componentRef.setInput('options', TEST_OPTIONS);
        fixture.detectChanges();

        component['openMenu']();
        fixture.detectChanges();

        expect(component['activeOptionId']()).toBe(`${component['id']()}-option-0`);
    });

    it('should show placeholder only while focused without selected value', async () => {
        const { component, fixture } = await setupSelectAsync();
        fixture.componentRef.setInput('placeholder', 'Choose fruit');
        fixture.detectChanges();

        expect(component['selectedLabel']()).toBe('');

        component['onFocus']();

        expect(component['selectedLabel']()).toBe('Choose fruit');
        expect(component['shouldFloatLabel']()).toBe(true);
    });
});

describe('FdUiSelectComponent classes', () => {
    it('should apply size class', async () => {
        const { fixture, requireElement } = await setupSelectAsync();
        fixture.componentRef.setInput('size', 'lg');
        fixture.detectChanges();

        const selectEl = requireElement('.fd-ui-select');
        expect(selectEl.classList).toContain('fd-ui-select--size-lg');
    });

    it('should not apply error class when error is omitted', async () => {
        const { requireElement } = await setupSelectAsync();

        const selectEl = requireElement('.fd-ui-select');
        expect(selectEl.classList).not.toContain('fd-ui-select--has-error');
    });

    it('should not apply error class when error is empty', async () => {
        const { fixture, requireElement } = await setupSelectAsync();
        fixture.componentRef.setInput('error', '');
        fixture.detectChanges();

        const selectEl = requireElement('.fd-ui-select');
        expect(selectEl.classList).not.toContain('fd-ui-select--has-error');
    });

    it('should apply error class when error is provided', async () => {
        const { fixture, requireElement } = await setupSelectAsync();
        fixture.componentRef.setInput('error', 'Required');
        fixture.detectChanges();

        const selectEl = requireElement('.fd-ui-select');
        expect(selectEl.classList).toContain('fd-ui-select--has-error');
    });
});

describe('FdUiSelectComponent overlay', () => {
    it('should focus listbox when overlay attaches', async () => {
        const { component, fixture } = await setupSelectAsync();
        const focus = vi.fn();
        Object.defineProperty(component, 'listboxRef', {
            value: vi.fn(() => ({
                nativeElement: { focus },
            })),
        });

        component['onMenuAttached']();
        await fixture.whenStable();

        expect(focus).toHaveBeenCalled();
    });

    it('should open and close menu from toggle', async () => {
        const { component, fixture } = await setupSelectAsync();
        fixture.componentRef.setInput('options', TEST_OPTIONS);
        fixture.detectChanges();
        const event = new Event('click');
        const preventDefaultSpy = vi.spyOn(event, 'preventDefault');

        component['toggleMenu'](event);
        fixture.detectChanges();

        expect(component['isOpen']()).toBe(true);
        expect(component['isFocused']()).toBe(true);

        component['toggleMenu'](event);

        expect(preventDefaultSpy).toHaveBeenCalled();
        expect(component['isOpen']()).toBe(false);
        expect(component['isFocused']()).toBe(false);
    });

    it('should keep focus on blur while menu is open and touch when closed', async () => {
        const { component, fixture } = await setupSelectAsync();
        fixture.componentRef.setInput('options', TEST_OPTIONS);
        fixture.detectChanges();
        component['openMenu']();
        component['onBlur']();

        expect(component['isFocused']()).toBe(true);
        expect(component.touched()).toBe(false);

        component['closeMenu']();
        component['onBlur']();

        expect(component['isFocused']()).toBe(false);
        expect(component.touched()).toBe(true);
    });

    it('should navigate listbox with arrow, home, end and select active option', async () => {
        const { component, fixture } = await setupSelectAsync();
        fixture.componentRef.setInput('options', TEST_OPTIONS);
        fixture.detectChanges();
        component['openMenu']();
        component['onListboxKeydown'](new KeyboardEvent('keydown', { key: 'ArrowDown' }));
        expect(component['activeIndex']()).toBe(1);

        component['onListboxKeydown'](new KeyboardEvent('keydown', { key: 'End' }));
        expect(component['activeIndex']()).toBe(2);

        component['onListboxKeydown'](new KeyboardEvent('keydown', { key: 'Home' }));
        expect(component['activeIndex']()).toBe(0);

        component['onListboxKeydown'](new KeyboardEvent('keydown', { key: 'Enter' }));

        expect(component.value()).toBe('apple');
        expect(component['isOpen']()).toBe(false);
    });

    it('should ignore listbox keyboard navigation when options are empty', async () => {
        const { component } = await setupSelectAsync();
        component['openMenu']();

        component['onListboxKeydown'](new KeyboardEvent('keydown', { key: 'ArrowDown' }));

        expect(component['activeIndex']()).toBe(0);
    });
});

describe('FdUiSelectComponent keyboard and wrapper interactions', () => {
    it('should open from control keyboard shortcuts and close on escape', async () => {
        const { component, fixture } = await setupSelectAsync();
        fixture.componentRef.setInput('options', TEST_OPTIONS);
        fixture.detectChanges();
        const openEvent = new KeyboardEvent('keydown', { key: 'ArrowUp' });
        const closeEvent = new KeyboardEvent('keydown', { key: 'Escape' });
        const openPreventDefaultSpy = vi.spyOn(openEvent, 'preventDefault');
        const closePreventDefaultSpy = vi.spyOn(closeEvent, 'preventDefault');

        component['onControlKeydown'](openEvent);

        expect(openPreventDefaultSpy).toHaveBeenCalledOnce();
        expect(component['isOpen']()).toBe(true);

        component['onControlKeydown'](closeEvent);

        expect(closePreventDefaultSpy).toHaveBeenCalledOnce();
        expect(component['isOpen']()).toBe(false);
    });

    it('should wrap listbox navigation and select with space', async () => {
        const { component, fixture } = await setupSelectAsync();
        fixture.componentRef.setInput('options', TEST_OPTIONS);
        fixture.detectChanges();
        component['openMenu']();
        component['onListboxKeydown'](new KeyboardEvent('keydown', { key: 'ArrowUp' }));
        expect(component['activeIndex']()).toBe(2);

        component['onListboxKeydown'](new KeyboardEvent('keydown', { key: ' ' }));

        expect(component.value()).toBe('cherry');
        expect(component['isOpen']()).toBe(false);
    });

    it('should close listbox on escape and return focus to the control', async () => {
        const { component, fixture } = await setupSelectAsync();
        fixture.componentRef.setInput('options', TEST_OPTIONS);
        fixture.detectChanges();
        const focus = vi.fn();
        Object.defineProperty(component, 'controlRef', {
            value: vi.fn(() => ({
                nativeElement: { focus },
            })),
        });

        component['openMenu']();
        component['onListboxKeydown'](new KeyboardEvent('keydown', { key: 'Escape' }));

        expect(component['isOpen']()).toBe(false);
        expect(focus).toHaveBeenCalledOnce();
    });

    it('should ignore unknown listbox keys without preventing default', async () => {
        const { component, fixture } = await setupSelectAsync();
        fixture.componentRef.setInput('options', TEST_OPTIONS);
        fixture.detectChanges();
        const event = new KeyboardEvent('keydown', { key: 'Tab' });
        const preventDefaultSpy = vi.spyOn(event, 'preventDefault');

        component['openMenu']();
        component['onListboxKeydown'](event);

        expect(preventDefaultSpy).not.toHaveBeenCalled();
        expect(component['activeIndex']()).toBe(0);
    });
});

describe('FdUiSelectComponent control wrapper interactions', () => {
    it('should toggle menu from control wrapper clicks outside the native button', async () => {
        const { component, fixture, requireElement } = await setupSelectAsync();
        fixture.componentRef.setInput('options', TEST_OPTIONS);
        fixture.detectChanges();
        const control = requireElement('.fd-ui-select__control') as HTMLButtonElement;
        const focusSpy = vi.spyOn(control, 'focus');
        const wrapper = requireElement('.fd-ui-select__control-wrap');
        const event = new MouseEvent('click', { bubbles: true });

        Object.defineProperty(event, 'target', { value: wrapper });
        component['onControlWrapClick'](event);

        expect(focusSpy).toHaveBeenCalledOnce();
        expect(component['isOpen']()).toBe(true);
    });

    it('should ignore control wrapper clicks from the native button and when disabled', async () => {
        const { component, fixture, requireElement } = await setupSelectAsync();
        fixture.componentRef.setInput('options', TEST_OPTIONS);
        fixture.detectChanges();
        const control = requireElement('.fd-ui-select__control');
        const buttonEvent = new MouseEvent('click', { bubbles: true });
        Object.defineProperty(buttonEvent, 'target', { value: control });

        component['onControlWrapClick'](buttonEvent);

        expect(component['isOpen']()).toBe(false);

        fixture.componentRef.setInput('disabled', true);
        fixture.detectChanges();
        const wrapperEvent = new MouseEvent('click', { bubbles: true });
        Object.defineProperty(wrapperEvent, 'target', { value: requireElement('.fd-ui-select__control-wrap') });
        component['onControlWrapClick'](wrapperEvent);

        expect(component['isOpen']()).toBe(false);
    });
});
