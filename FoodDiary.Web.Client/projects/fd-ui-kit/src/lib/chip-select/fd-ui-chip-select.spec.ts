import { TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { FdUiChipSelectComponent } from './fd-ui-chip-select';

describe('FdUiChipSelectComponent', () => {
    it('renders the selection as pressed buttons and emits a new selection when toggled', () => {
        const fixture = TestBed.createComponent(FdUiChipSelectComponent);
        const initial = ['breakfast'];
        fixture.componentRef.setInput('ariaLabel', 'Meal types');
        fixture.componentRef.setInput('size', 'sm');
        fixture.componentRef.setInput('options', [
            { value: 'breakfast', label: 'Breakfast' },
            { value: 'lunch', label: 'Lunch', ariaLabel: 'Lunch dishes' },
            { value: 'dinner', label: 'Dinner', disabled: true },
        ]);
        fixture.componentRef.setInput('selectedValues', initial);
        fixture.detectChanges();
        const changed = vi.fn();
        fixture.componentInstance.selectedValues.subscribe(changed);
        const host = fixture.nativeElement as HTMLElement;
        const group = host.querySelector('[role="group"]');
        const [breakfast, lunch, dinner] = host.querySelectorAll('button');

        expect(group?.getAttribute('aria-label')).toBe('Meal types');
        expect(group?.classList.contains('fd-ui-chip-select--size-sm')).toBe(true);
        expect(breakfast.getAttribute('aria-pressed')).toBe('true');
        expect(lunch.getAttribute('aria-label')).toBe('Lunch dishes');
        expect(dinner.disabled).toBe(true);

        dinner.click();
        expect(changed).not.toHaveBeenCalled();
        lunch.click();
        fixture.detectChanges();
        expect(lunch.getAttribute('aria-pressed')).toBe('true');
        breakfast.click();
        fixture.detectChanges();

        expect(breakfast.getAttribute('aria-pressed')).toBe('false');
        expect(changed.mock.calls).toEqual([[['breakfast', 'lunch']], [['lunch']]]);
        expect(initial).toEqual(['breakfast']);
    });
});
