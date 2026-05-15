import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { GoalsCalorieCardComponent } from './goals-calorie-card.component';

const MIN_CALORIES = 0;
const MAX_CALORIES = 4000;
const CALORIE_TARGET = 2100;

describe('GoalsCalorieCardComponent', () => {
    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [GoalsCalorieCardComponent, TranslateModule.forRoot()],
        });
    });

    it('renders calorie target and ring style inputs', () => {
        const fixture = createComponent();
        const element = getElement(fixture);
        const ring = getRequiredElement(element, '.goals__ring');
        const input = getRequiredElement(element, '.goals__calorie-input') as HTMLInputElement;

        expect(input.value).toBe(CALORIE_TARGET.toString());
        expect(input.min).toBe(MIN_CALORIES.toString());
        expect(input.max).toBe(MAX_CALORIES.toString());
        expect(ring.style.getPropertyValue('--goals-progress')).toBe('52%');
        expect(ring.style.getPropertyValue('--goals-accent')).toBe('var(--fd-color-green-500)');
        expect(ring.style.getPropertyValue('--goals-angle')).toBe('187deg');
    });

    it('emits ring and input events', () => {
        const fixture = createComponent();
        const ringPointerDown = vi.fn();
        const ringPointerMove = vi.fn();
        const ringPointerLeave = vi.fn();
        const caloriesInput = vi.fn();
        const caloriesBlur = vi.fn();
        const sliderInput = vi.fn();
        fixture.componentInstance.ringPointerDown.subscribe(ringPointerDown);
        fixture.componentInstance.ringPointerMove.subscribe(ringPointerMove);
        fixture.componentInstance.ringPointerLeave.subscribe(ringPointerLeave);
        fixture.componentInstance.caloriesInput.subscribe(caloriesInput);
        fixture.componentInstance.caloriesBlur.subscribe(caloriesBlur);
        fixture.componentInstance.sliderInput.subscribe(sliderInput);
        const element = getElement(fixture);

        getRequiredElement(element, '.goals__ring').dispatchEvent(new PointerEvent('pointerdown'));
        getRequiredElement(element, '.goals__ring').dispatchEvent(new PointerEvent('pointermove'));
        getRequiredElement(element, '.goals__ring').dispatchEvent(new PointerEvent('pointerleave'));
        getRequiredElement(element, '.goals__calorie-input').dispatchEvent(new Event('input'));
        getRequiredElement(element, '.goals__calorie-input').dispatchEvent(new Event('blur'));
        getRequiredElement(element, '.goals__range').dispatchEvent(new Event('input'));

        expect(ringPointerDown).toHaveBeenCalledTimes(1);
        expect(ringPointerMove).toHaveBeenCalledTimes(1);
        expect(ringPointerLeave).toHaveBeenCalledTimes(1);
        expect(caloriesInput).toHaveBeenCalledTimes(1);
        expect(caloriesBlur).toHaveBeenCalledTimes(1);
        expect(sliderInput).toHaveBeenCalledTimes(1);
    });
});

function createComponent(): ComponentFixture<GoalsCalorieCardComponent> {
    const fixture = TestBed.createComponent(GoalsCalorieCardComponent);
    fixture.componentRef.setInput('minCalories', MIN_CALORIES);
    fixture.componentRef.setInput('maxCalories', MAX_CALORIES);
    fixture.componentRef.setInput('calorieTarget', CALORIE_TARGET);
    fixture.componentRef.setInput('ringProgressOffset', '52%');
    fixture.componentRef.setInput('accentColor', 'var(--fd-color-green-500)');
    fixture.componentRef.setInput('ringKnobAngle', '187deg');
    fixture.detectChanges();

    return fixture;
}

function getElement(fixture: ComponentFixture<GoalsCalorieCardComponent>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}

function getRequiredElement(element: HTMLElement, selector: string): HTMLElement {
    const target = element.querySelector<HTMLElement>(selector);
    if (target === null) {
        throw new Error(`Element "${selector}" was not found.`);
    }

    return target;
}
