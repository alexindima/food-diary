import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import type { CyclingDayControl } from '../../goals-page-lib/goals-page.models';
import { GoalsCyclingCardComponent } from './goals-cycling-card.component';

const MONDAY_CALORIES = 2100;
const TUESDAY_CALORIES = 2200;
const MAX_CALORIES = 4000;

describe('GoalsCyclingCardComponent', () => {
    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [GoalsCyclingCardComponent, TranslateModule.forRoot()],
        });
    });

    it('renders only toggle when disabled', () => {
        const fixture = createComponent({ enabled: false });
        const element = getElement(fixture);

        expect(element.textContent).toContain('GOALS_PAGE.CYCLING_ENABLE');
        expect(element.querySelector('.goals__cycling-days')).toBeNull();
    });

    it('renders day calorie inputs when enabled', () => {
        const fixture = createComponent();
        const inputs = getElement(fixture).querySelectorAll<HTMLInputElement>('.goals__cycling-day-input');

        expect(inputs).toHaveLength(2);
        expect(inputs[0].value).toBe(MONDAY_CALORIES.toString());
        expect(inputs[1].value).toBe(TUESDAY_CALORIES.toString());
        expect(inputs[0].max).toBe(MAX_CALORIES.toString());
    });

    it('emits toggle and day input events', () => {
        const fixture = createComponent();
        const enabledToggle = vi.fn();
        const dayCaloriesInput = vi.fn();
        fixture.componentInstance.enabledToggle.subscribe(enabledToggle);
        fixture.componentInstance.dayCaloriesInput.subscribe(dayCaloriesInput);
        const element = getElement(fixture);

        element.querySelector<HTMLInputElement>('input[type="checkbox"]')?.dispatchEvent(new Event('change'));
        element.querySelector<HTMLInputElement>('.goals__cycling-day-input')?.dispatchEvent(new Event('input'));

        expect(enabledToggle).toHaveBeenCalledTimes(1);
        expect(dayCaloriesInput).toHaveBeenCalledWith(expect.objectContaining({ key: 'mondayCalories' }));
    });
});

function createComponent(overrides: Partial<{ enabled: boolean }> = {}): ComponentFixture<GoalsCyclingCardComponent> {
    const fixture = TestBed.createComponent(GoalsCyclingCardComponent);
    fixture.componentRef.setInput('enabled', overrides.enabled ?? true);
    fixture.componentRef.setInput('dayControls', createDayControls());
    fixture.componentRef.setInput('dayCalories', {
        mondayCalories: MONDAY_CALORIES,
        tuesdayCalories: TUESDAY_CALORIES,
    });
    fixture.componentRef.setInput('maxCalories', MAX_CALORIES);
    fixture.detectChanges();

    return fixture;
}

function getElement(fixture: ComponentFixture<GoalsCyclingCardComponent>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}

function createDayControls(): CyclingDayControl[] {
    return [
        { key: 'mondayCalories', labelKey: 'GOALS_PAGE.DAY_MONDAY', inputId: 'cycling-day-mondayCalories' },
        { key: 'tuesdayCalories', labelKey: 'GOALS_PAGE.DAY_TUESDAY', inputId: 'cycling-day-tuesdayCalories' },
    ];
}
