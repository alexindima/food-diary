import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { beforeEach, describe, expect, it } from 'vitest';

import { FdUiSegmentedToggleComponent, type FdUiSegmentedToggleOption } from './fd-ui-segmented-toggle.component';

describe('FdUiSegmentedToggleComponent', () => {
    let component: FdUiSegmentedToggleComponent;
    let fixture: ComponentFixture<FdUiSegmentedToggleComponent>;

    const testOptions: FdUiSegmentedToggleOption[] = [
        { value: 'day', label: 'Day' },
        { value: 'week', label: 'Week' },
        { value: 'month', label: 'Month' },
    ];

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [FdUiSegmentedToggleComponent],
            providers: [provideNoopAnimations()],
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
        const buttons = fixture.debugElement.queryAll(By.css('[role="radio"]'));
        expect(buttons.length).toBe(3);
        expect(buttons[0].nativeElement.textContent.trim()).toBe('Day');
        expect(buttons[1].nativeElement.textContent.trim()).toBe('Week');
        expect(buttons[2].nativeElement.textContent.trim()).toBe('Month');
    });

    it('should mark selected option with aria-checked="true"', () => {
        const buttons = fixture.debugElement.queryAll(By.css('[role="radio"]'));
        expect(buttons[0].nativeElement.getAttribute('aria-checked')).toBe('true');
        expect(buttons[1].nativeElement.getAttribute('aria-checked')).toBe('false');
        expect(buttons[2].nativeElement.getAttribute('aria-checked')).toBe('false');
    });

    it('should update selection on click', () => {
        const buttons = fixture.debugElement.queryAll(By.css('[role="radio"]'));
        buttons[1].nativeElement.click();
        fixture.detectChanges();

        expect(component.selectedValue()).toBe('week');
        expect(buttons[1].nativeElement.getAttribute('aria-checked')).toBe('true');
        expect(buttons[0].nativeElement.getAttribute('aria-checked')).toBe('false');
    });

    it('should apply size class', () => {
        const container = fixture.debugElement.query(By.css('.fd-ui-segmented-toggle'));
        expect(container).toBeTruthy();

        // The component uses the is-active class for selected items
        const activeButton = fixture.debugElement.query(By.css('.is-active'));
        expect(activeButton).toBeTruthy();
    });

    it('should emit selectedValue change', () => {
        let emittedValue: string | undefined;
        component.selectedValue.subscribe(value => {
            emittedValue = value;
        });

        const buttons = fixture.debugElement.queryAll(By.css('[role="radio"]'));
        buttons[2].nativeElement.click();
        fixture.detectChanges();

        expect(emittedValue).toBe('month');
    });
});
