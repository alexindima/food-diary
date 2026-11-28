import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideTranslateService, TranslateService } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { FdUiSegmentedToggleComponent, type FdUiSegmentedToggleOption } from './fd-ui-segmented-toggle';

const OPTION_COUNT = 3;
const testOptions: FdUiSegmentedToggleOption[] = [
    { value: 'day', label: 'Day' },
    { value: 'week', label: 'Week' },
    { value: 'month', label: 'Month' },
];

let component: FdUiSegmentedToggleComponent;
let fixture: ComponentFixture<FdUiSegmentedToggleComponent>;

function host(): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}

function radioButtons(): HTMLButtonElement[] {
    return Array.from(host().querySelectorAll<HTMLButtonElement>('[role="radio"]'));
}

function requireElement(selector: string): HTMLElement {
    const element = host().querySelector<HTMLElement>(selector);
    if (element === null) {
        throw new Error(`Expected element ${selector} to exist.`);
    }

    return element;
}

beforeEach(async () => {
    await TestBed.configureTestingModule({
        imports: [FdUiSegmentedToggleComponent],
        providers: [provideTranslateService()],
    }).compileComponents();

    fixture = TestBed.createComponent(FdUiSegmentedToggleComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('options', testOptions);
    fixture.componentRef.setInput('selectedValue', 'day');
    fixture.detectChanges();
});

describe('FdUiSegmentedToggleComponent changing inputs', () => {
    it('refreshes translated option keys after the language changes and preserves explicit labels and selection', async () => {
        const translate = TestBed.inject(TranslateService);
        translate.setTranslation('en', { PERIOD: { DAY: 'Today', WEEK: 'This week' } });
        translate.setTranslation('ru', { PERIOD: { DAY: 'Сегодня', WEEK: 'Эта неделя' } });
        await firstValueFrom(translate.use('en'));
        fixture.componentRef.setInput('options', [
            { value: 'day', labelKey: 'PERIOD.DAY' },
            { value: 'week', label: 'Pinned caption', labelKey: 'PERIOD.WEEK' },
            { value: 'month' },
        ]);
        fixture.detectChanges();
        expect(radioButtons().map(button => button.textContent.trim())).toEqual(['Today', 'Pinned caption', '']);

        await firstValueFrom(translate.use('ru'));
        fixture.detectChanges();
        expect(radioButtons().map(button => button.textContent.trim())).toEqual(['Сегодня', 'Pinned caption', '']);
        expect(radioButtons()[0].getAttribute('aria-checked')).toBe('true');
    });

    it('applies full-width and shrink behavior to the rendered control and removes both on reset', () => {
        fixture.componentRef.setInput('fullWidth', true);
        fixture.componentRef.setInput('shrinkItems', true);
        fixture.detectChanges();

        const container = requireElement('.fd-ui-segmented-toggle');
        expect(host().classList).toContain('fd-ui-segmented-toggle-host--full-width');
        expect(container.classList).toContain('fd-ui-segmented-toggle--full-width');
        expect(container.classList).toContain('fd-ui-segmented-toggle--shrink-items');

        fixture.componentRef.setInput('fullWidth', false);
        fixture.componentRef.setInput('shrinkItems', false);
        fixture.detectChanges();
        expect(host().classList).not.toContain('fd-ui-segmented-toggle-host--full-width');
        expect(container.classList).not.toContain('fd-ui-segmented-toggle--full-width');
        expect(container.classList).not.toContain('fd-ui-segmented-toggle--shrink-items');
    });

    it('emits only when the selection changes', () => {
        const changed = vi.fn<(value: string) => void>();
        component.selectedValue.subscribe(changed);
        const buttons = radioButtons();
        buttons[0].click();
        expect(changed).not.toHaveBeenCalled();

        buttons[1].click();
        buttons[1].click();
        fixture.detectChanges();
        expect(changed).toHaveBeenCalledExactlyOnceWith('week');
        expect(buttons[1].getAttribute('aria-checked')).toBe('true');
    });
});

describe('FdUiSegmentedToggleComponent keyboard navigation', () => {
    it('moves focus and selection with arrow keys and wraps at the group edges', () => {
        const changed = vi.fn<(value: string) => void>();
        component.selectedValue.subscribe(changed);
        const [day, week, month] = radioButtons();
        expect([day.tabIndex, week.tabIndex, month.tabIndex]).toEqual([0, -1, -1]);
        day.focus();

        pressArrow(day, 'ArrowLeft');
        expect(document.activeElement).toBe(month);
        expect([day.tabIndex, week.tabIndex, month.tabIndex]).toEqual([-1, -1, 0]);
        expect(month.getAttribute('aria-checked')).toBe('true');
        pressArrow(month, 'ArrowDown');
        expect(document.activeElement).toBe(day);
        pressArrow(day, 'ArrowRight');
        expect(document.activeElement).toBe(week);
        pressArrow(week, 'ArrowUp');
        expect(document.activeElement).toBe(day);
        expect(changed.mock.calls).toEqual([['month'], ['day'], ['week'], ['day']]);
    });

    it('uses the checked option as the only tab stop and leaves a fallback when it is absent', () => {
        const changed = vi.fn<(value: string) => void>();
        component.selectedValue.subscribe(changed);
        fixture.componentRef.setInput('selectedValue', 'month');
        fixture.detectChanges();
        expect(radioButtons().map(button => button.tabIndex)).toEqual([-1, -1, 0]);

        fixture.componentRef.setInput('options', testOptions.slice(0, -1));
        fixture.detectChanges();
        expect(radioButtons().map(button => button.tabIndex)).toEqual([0, -1]);
        expect(radioButtons().map(button => button.getAttribute('aria-checked'))).toEqual(['false', 'false']);
        expect(changed).not.toHaveBeenCalled();
    });

    it('leaves Tab to the browser without selecting another radio', () => {
        const [day] = radioButtons();
        day.focus();
        expect(pressKey(day, 'Tab').defaultPrevented).toBe(false);
        expect(document.activeElement).toBe(day);
        expect(day.getAttribute('aria-checked')).toBe('true');
    });

    it('ignores navigation from the focused option if an update has removed it before rendering', () => {
        const changed = vi.fn<(value: string) => void>();
        component.selectedValue.subscribe(changed);
        const [day] = radioButtons();
        day.focus();
        fixture.componentRef.setInput('options', testOptions.slice(1));

        expect(pressKey(day, 'ArrowRight').defaultPrevented).toBe(false);
        fixture.detectChanges();

        expect(day.isConnected).toBe(false);
        expect(radioButtons()[0].tabIndex).toBe(0);
        expect(changed).not.toHaveBeenCalled();
    });

    it('waits to select a newly added option until its radio can receive focus', () => {
        const changed = vi.fn<(value: string) => void>();
        component.selectedValue.subscribe(changed);
        const [day] = radioButtons();
        day.focus();
        fixture.componentRef.setInput('options', [testOptions[0], { value: 'year', label: 'Year' }]);

        expect(pressKey(day, 'ArrowRight').defaultPrevented).toBe(false);
        expect(document.activeElement).toBe(day);
        expect(changed).not.toHaveBeenCalled();
        fixture.detectChanges();

        pressArrow(day, 'ArrowRight');
        expect(document.activeElement).toBe(radioButtons()[1]);
        expect(changed).toHaveBeenCalledExactlyOnceWith('year');
    });
});

function pressKey(button: HTMLButtonElement, key: string): KeyboardEvent {
    const event = new KeyboardEvent('keydown', { key, bubbles: true, cancelable: true });
    button.dispatchEvent(event);
    return event;
}

function pressArrow(button: HTMLButtonElement, key: string): void {
    const event = pressKey(button, key);
    fixture.detectChanges();
    expect(event.defaultPrevented).toBe(true);
}

describe('FdUiSegmentedToggleComponent', () => {
    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should render all options', () => {
        const buttons = radioButtons();
        expect(buttons.length).toBe(OPTION_COUNT);
        expect(buttons[0].textContent.trim()).toBe('Day');
        expect(buttons[1].textContent.trim()).toBe('Week');
        expect(buttons[2].textContent.trim()).toBe('Month');
    });

    it('should mark selected option with aria-checked="true"', () => {
        const buttons = radioButtons();
        expect(buttons[0].getAttribute('aria-checked')).toBe('true');
        expect(buttons[1].getAttribute('aria-checked')).toBe('false');
        expect(buttons[2].getAttribute('aria-checked')).toBe('false');
    });

    it('should update selection on click', () => {
        const buttons = radioButtons();
        buttons[1].click();
        fixture.detectChanges();

        expect(component['selectedValue']()).toBe('week');
        expect(buttons[1].getAttribute('aria-checked')).toBe('true');
        expect(buttons[0].getAttribute('aria-checked')).toBe('false');
    });

    it('should apply selected option class', () => {
        const container = host().querySelector('.fd-ui-segmented-toggle');
        expect(container).toBeTruthy();

        const activeButton = host().querySelector('.is-active');
        expect(activeButton).toBeTruthy();
    });

    it('should apply size class', () => {
        fixture.componentRef.setInput('size', 'sm');
        fixture.detectChanges();

        const container = requireElement('.fd-ui-segmented-toggle');
        expect(container.classList).toContain('fd-ui-segmented-toggle--size-sm');
    });

    it('should control narrow stacking class', () => {
        const container = requireElement('.fd-ui-segmented-toggle');
        expect(container.classList).toContain('fd-ui-segmented-toggle--stack-on-narrow');

        fixture.componentRef.setInput('stackOnNarrow', false);
        fixture.detectChanges();
        expect(container.classList).not.toContain('fd-ui-segmented-toggle--stack-on-narrow');
    });

    it('should apply narrow wrapping when requested', () => {
        fixture.componentRef.setInput('wrapOnNarrow', true);
        fixture.detectChanges();

        expect(requireElement('.fd-ui-segmented-toggle').classList).toContain('fd-ui-segmented-toggle--wrap-on-narrow');
    });

    it('should size the host to its content when requested', () => {
        fixture.componentRef.setInput('fitContent', true);
        fixture.detectChanges();

        expect(host().classList).toContain('fd-ui-segmented-toggle-host--fit-content');
    });

    it('should emit selectedValue change', () => {
        let emittedValue: string | undefined;
        component['selectedValue'].subscribe(value => {
            emittedValue = value;
        });

        const buttons = radioButtons();
        buttons[2].click();
        fixture.detectChanges();

        expect(emittedValue).toBe('month');
    });
});
