import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { FdUiEmojiPickerComponent, type FdUiEmojiPickerOption, type FdUiEmojiPickerValue } from './fd-ui-emoji-picker';

const OPTIONS: FdUiEmojiPickerOption[] = [
    { value: 'low', emoji: '😫', label: 'Low', ariaLabel: 'Low energy' },
    { value: 'unavailable', emoji: '😟', label: 'Unavailable', disabled: true },
    { value: 'steady', emoji: '😐', label: 'Steady' },
    { value: 'high', emoji: '🙂', ariaLabel: 'High energy' },
];
const EDGE_DISABLED_OPTIONS: FdUiEmojiPickerOption[] = [
    { value: 'disabled-first', emoji: '😴', label: 'First unavailable', disabled: true },
    ...OPTIONS,
    { value: 'disabled-last', emoji: '😴', label: 'Last unavailable', disabled: true },
];

type EmojiPickerHarness = {
    fixture: ComponentFixture<FdUiEmojiPickerComponent>;
    component: FdUiEmojiPickerComponent;
    root: HTMLElement;
    changed: ReturnType<typeof vi.fn<(value: FdUiEmojiPickerValue | null) => void>>;
};

let current: EmojiPickerHarness | undefined;

afterEach(() => {
    current?.fixture.destroy();
    current?.root.remove();
    current = undefined;
});

function createHarness(selected: FdUiEmojiPickerValue | null = null, options = OPTIONS): EmojiPickerHarness {
    TestBed.configureTestingModule({ imports: [FdUiEmojiPickerComponent] });
    const fixture = TestBed.createComponent(FdUiEmojiPickerComponent);
    fixture.componentRef.setInput('options', options);
    fixture.componentRef.setInput('selectedValue', selected);
    fixture.componentRef.setInput('ariaLabel', 'Energy level');
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;
    document.body.append(root);
    const changed = vi.fn<(value: FdUiEmojiPickerValue | null) => void>();
    fixture.componentInstance.selectedValue.subscribe(changed);
    current = { fixture, component: fixture.componentInstance, root, changed };
    return current;
}

function radios(harness: EmojiPickerHarness): HTMLButtonElement[] {
    return [...harness.root.querySelectorAll<HTMLButtonElement>('[role="radio"]')];
}

function radio(harness: EmojiPickerHarness, label: string): HTMLButtonElement {
    const result = radios(harness).find(button => button.getAttribute('aria-label') === label);
    if (result === undefined) {
        throw new Error(`Expected the rendered emoji option "${label}".`);
    }
    return result;
}

function pressKey(harness: EmojiPickerHarness, button: HTMLButtonElement, key: string): KeyboardEvent {
    const event = new KeyboardEvent('keydown', { key, bubbles: true, cancelable: true });
    button.dispatchEvent(event);
    harness.fixture.detectChanges();
    return event;
}

function expectSelection(harness: EmojiPickerHarness, selected: HTMLButtonElement): void {
    for (const button of radios(harness)) {
        expect(button.getAttribute('aria-checked')).toBe(String(button === selected));
        expect(button.tabIndex).toBe(button === selected ? 0 : -1);
        expect(button.classList.contains('is-selected')).toBe(button === selected);
    }
}

describe('FdUiEmojiPickerComponent rendered selection', () => {
    it('names the radio group and offers the first enabled option as its only tab stop without selecting it', () => {
        const harness = createHarness(null, EDGE_DISABLED_OPTIONS);
        expect(harness.root.querySelector('[role="radiogroup"]')?.getAttribute('aria-label')).toBe('Energy level');
        expect(radios(harness).filter(button => !button.disabled && button.tabIndex === 0)).toEqual([radio(harness, 'Low energy')]);
        expect(radios(harness).every(button => button.getAttribute('aria-checked') === 'false')).toBe(true);
        expect(harness.component.selectedValue()).toBeNull();
        expect(harness.changed).not.toHaveBeenCalled();
    });

    it('selects a clicked option once and ignores repeated activation of the same value', () => {
        const harness = createHarness('low');
        const target = radio(harness, 'Steady');
        target.click();
        target.click();
        harness.fixture.detectChanges();
        expect(harness.component.selectedValue()).toBe('steady');
        expect(harness.changed).toHaveBeenCalledExactlyOnceWith('steady');
        expectSelection(harness, target);
    });

    it('preserves numeric and string option identities and exposes an accessible fallback name for zero', () => {
        const harness = createHarness(1, [
            { value: 0, emoji: '😴' },
            { value: 1, emoji: '🙂', ariaLabel: 'Numeric one' },
            { value: '1', emoji: '😐', ariaLabel: 'Text one' },
        ]);
        expectSelection(harness, radio(harness, 'Numeric one'));
        expect(radio(harness, '0').getAttribute('aria-label')).toBe('0');
        radio(harness, 'Text one').click();
        harness.fixture.detectChanges();
        expect(harness.changed).toHaveBeenCalledExactlyOnceWith('1');
        expect(harness.component.selectedValue()).toBe('1');
        expectSelection(harness, radio(harness, 'Text one'));
    });

    it('reflects an externally changed model without emitting a user selection', () => {
        const harness = createHarness('low');
        harness.fixture.componentRef.setInput('selectedValue', 'high');
        harness.fixture.detectChanges();
        expectSelection(harness, radio(harness, 'High energy'));
        expect(harness.changed).not.toHaveBeenCalled();
    });
});

describe('FdUiEmojiPickerComponent arrow navigation', () => {
    it.each([
        { key: 'ArrowRight', from: 'Low energy', initial: 'low', to: 'Steady', value: 'steady' },
        { key: 'ArrowDown', from: 'Low energy', initial: 'low', to: 'Steady', value: 'steady' },
        { key: 'ArrowLeft', from: 'Steady', initial: 'steady', to: 'Low energy', value: 'low' },
        { key: 'ArrowUp', from: 'Steady', initial: 'steady', to: 'Low energy', value: 'low' },
    ])('moves focus and selection with $key while skipping disabled options', ({ key, from, initial, to, value }) => {
        const harness = createHarness(initial);
        const start = radio(harness, from);
        start.focus();
        const event = pressKey(harness, start, key);
        const selected = radio(harness, to);
        expect(event.defaultPrevented).toBe(true);
        expect(document.activeElement).toBe(selected);
        expect(harness.component.selectedValue()).toBe(value);
        expect(harness.changed).toHaveBeenCalledExactlyOnceWith(value);
        expectSelection(harness, selected);
    });

    it.each([
        { key: 'ArrowLeft', label: 'Low energy', value: 'low' },
        { key: 'ArrowRight', label: 'High energy', value: 'high' },
    ])('keeps focus and selection at the available boundary for $key', ({ key, label, value }) => {
        const harness = createHarness(value);
        const selected = radio(harness, label);
        selected.focus();
        expect(pressKey(harness, selected, key).defaultPrevented).toBe(true);
        expect(document.activeElement).toBe(selected);
        expect(harness.component.selectedValue()).toBe(value);
        expect(harness.changed).not.toHaveBeenCalled();
        expectSelection(harness, selected);
    });
});

describe('FdUiEmojiPickerComponent keyboard activation', () => {
    it.each([
        { key: 'Home', label: 'Low energy', value: 'low' },
        { key: 'End', label: 'High energy', value: 'high' },
    ])('selects and focuses the enabled edge for $key', ({ key, label, value }) => {
        const harness = createHarness('steady', EDGE_DISABLED_OPTIONS);
        const start = radio(harness, 'Steady');
        start.focus();
        expect(pressKey(harness, start, key).defaultPrevented).toBe(true);
        const selected = radio(harness, label);
        expect(document.activeElement).toBe(selected);
        expect(harness.changed).toHaveBeenCalledExactlyOnceWith(value);
        expectSelection(harness, selected);
    });

    it.each(['Enter', ' '])('handles %j activation once even if the same button also receives a click', key => {
        const harness = createHarness();
        const selected = radio(harness, 'Steady');
        selected.focus();
        expect(pressKey(harness, selected, key).defaultPrevented).toBe(true);
        selected.click();
        harness.fixture.detectChanges();
        expect(harness.component.selectedValue()).toBe('steady');
        expect(harness.changed).toHaveBeenCalledExactlyOnceWith('steady');
        expect(document.activeElement).toBe(selected);
        expectSelection(harness, selected);
    });

    it.each(['Tab', 'Escape', 'a'])('leaves unrelated %s behavior to the browser', key => {
        const harness = createHarness('low');
        const selected = radio(harness, 'Low energy');
        selected.focus();
        expect(pressKey(harness, selected, key).defaultPrevented).toBe(false);
        expect(document.activeElement).toBe(selected);
        expect(harness.changed).not.toHaveBeenCalled();
        expectSelection(harness, selected);
    });
});

describe('FdUiEmojiPickerComponent disabled and empty options', () => {
    it('uses native disabled buttons and rejects disabled activation without altering selection', () => {
        const harness = createHarness('low');
        const disabled = radio(harness, 'Unavailable');
        expect(disabled.disabled).toBe(true);
        expect(disabled.tabIndex).toBe(-1);
        disabled.click();
        pressKey(harness, disabled, 'Enter');
        expect(harness.component.selectedValue()).toBe('low');
        expect(harness.changed).not.toHaveBeenCalled();
        expectSelection(harness, radio(harness, 'Low energy'));
    });

    it('renders no selectable controls for an empty list and becomes usable when options arrive', () => {
        const harness = createHarness(null, []);
        expect(radios(harness)).toEqual([]);
        expect(harness.root.querySelector('[role="radiogroup"]')).not.toBeNull();
        harness.fixture.componentRef.setInput('options', OPTIONS);
        harness.fixture.detectChanges();
        expect(radios(harness).filter(button => button.tabIndex === 0)).toEqual([radio(harness, 'Low energy')]);
        expect(harness.component.selectedValue()).toBeNull();
        expect(harness.changed).not.toHaveBeenCalled();
    });

    it('has no tab stop or user emissions when every option is disabled', () => {
        const harness = createHarness(
            null,
            OPTIONS.map(option => ({ ...option, disabled: true })),
        );
        for (const button of radios(harness)) {
            expect(button.disabled).toBe(true);
            expect(button.tabIndex).toBe(-1);
            button.click();
            pressKey(harness, button, 'Home');
            pressKey(harness, button, 'End');
        }
        expect(harness.component.selectedValue()).toBeNull();
        expect(harness.changed).not.toHaveBeenCalled();
    });
});

describe('FdUiEmojiPickerComponent dynamic tab stops', () => {
    it('restores an enabled tab stop after the selected option is removed without silently selecting a replacement', () => {
        const harness = createHarness('steady');
        harness.fixture.componentRef.setInput(
            'options',
            OPTIONS.filter(option => option.value !== 'steady'),
        );
        harness.fixture.detectChanges();
        const fallback = radio(harness, 'Low energy');
        expect(radios(harness).filter(button => !button.disabled && button.tabIndex === 0)).toEqual([fallback]);
        expect(radios(harness).every(button => button.getAttribute('aria-checked') === 'false')).toBe(true);
        expect(harness.component.selectedValue()).toBe('steady');
        expect(harness.changed).not.toHaveBeenCalled();
        fallback.focus();
        pressKey(harness, fallback, ' ');
        expect(harness.changed).toHaveBeenCalledExactlyOnceWith('low');
        expectSelection(harness, fallback);
    });

    it('restores an enabled tab stop after the selected option becomes disabled without emitting a replacement', () => {
        const harness = createHarness('steady');
        harness.fixture.componentRef.setInput(
            'options',
            OPTIONS.map(option => ({ ...option, disabled: option.value === 'steady' || option.disabled === true })),
        );
        harness.fixture.detectChanges();
        const fallback = radio(harness, 'Low energy');
        expect(radios(harness).filter(button => !button.disabled && button.tabIndex === 0)).toEqual([fallback]);
        expect(radio(harness, 'Steady').disabled).toBe(true);
        expect(radio(harness, 'Steady').tabIndex).toBe(-1);
        expect(radio(harness, 'Steady').getAttribute('aria-checked')).toBe('true');
        expect(harness.component.selectedValue()).toBe('steady');
        expect(harness.changed).not.toHaveBeenCalled();
        fallback.focus();
        pressKey(harness, fallback, 'Enter');
        expect(harness.changed).toHaveBeenCalledExactlyOnceWith('low');
        expectSelection(harness, fallback);
    });

    it('removes every tab stop while disabled and restores an entry point when another option is enabled', () => {
        const harness = createHarness('steady');
        harness.fixture.componentRef.setInput(
            'options',
            OPTIONS.map(option => ({ ...option, disabled: true })),
        );
        harness.fixture.detectChanges();
        expect(radios(harness).every(button => button.disabled && button.tabIndex === -1)).toBe(true);
        harness.fixture.componentRef.setInput(
            'options',
            OPTIONS.map(option => ({ ...option, disabled: option.value !== 'low' })),
        );
        harness.fixture.detectChanges();
        expect(radios(harness).filter(button => !button.disabled && button.tabIndex === 0)).toEqual([radio(harness, 'Low energy')]);
        expect(harness.component.selectedValue()).toBe('steady');
        expect(harness.changed).not.toHaveBeenCalled();
    });
});
