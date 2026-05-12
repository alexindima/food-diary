import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { FdUiSegmentedToggleComponent, type FdUiSegmentedToggleOption } from './fd-ui-segmented-toggle.component';

const OPTION_COUNT = 3;
const testOptions: FdUiSegmentedToggleOption[] = [
    { value: 'day', label: 'Day' },
    { value: 'week', label: 'Week' },
    { value: 'month', label: 'Month' },
];

describe('FdUiSegmentedToggleComponent', () => {
    let component: FdUiSegmentedToggleComponent;
    let fixture: ComponentFixture<FdUiSegmentedToggleComponent>;

    const host = (): HTMLElement => fixture.nativeElement as HTMLElement;
    const radioButtons = (): HTMLButtonElement[] => Array.from(host().querySelectorAll<HTMLButtonElement>('[role="radio"]'));
    const requireElement = <T extends Element>(selector: string): T => {
        const element = host().querySelector<T>(selector);
        if (element === null) {
            throw new Error(`Expected element ${selector} to exist.`);
        }

        return element;
    };

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [FdUiSegmentedToggleComponent],
            providers: [],
        }).compileComponents();

        fixture = TestBed.createComponent(FdUiSegmentedToggleComponent);
        component = fixture.componentInstance;
        fixture.componentRef.setInput('options', testOptions);
        fixture.componentRef.setInput('selectedValue', 'day');
        fixture.detectChanges();
    });

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

        expect(component.selectedValue()).toBe('week');
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

        const container = requireElement<HTMLElement>('.fd-ui-segmented-toggle');
        expect(container.classList).toContain('fd-ui-segmented-toggle--size-sm');
    });

    it('should control narrow stacking class', () => {
        const container = requireElement<HTMLElement>('.fd-ui-segmented-toggle');
        expect(container.classList).toContain('fd-ui-segmented-toggle--stack-on-narrow');

        fixture.componentRef.setInput('stackOnNarrow', false);
        fixture.detectChanges();
        expect(container.classList).not.toContain('fd-ui-segmented-toggle--stack-on-narrow');
    });

    it('should emit selectedValue change', () => {
        let emittedValue: string | undefined;
        component.selectedValue.subscribe(value => {
            emittedValue = value;
        });

        const buttons = radioButtons();
        buttons[2].click();
        fixture.detectChanges();

        expect(emittedValue).toBe('month');
    });
});
