import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import type { MacroSliderView } from '../../goals-page-lib/goals-page.models';
import { GoalsMacroSliderComponent } from './goals-macro-slider.component';

const MACRO_VALUE = 120;
const MACRO_MAX = 220;
const MACRO_RATIO = 0.55;

describe('GoalsMacroSliderComponent', () => {
    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [GoalsMacroSliderComponent, TranslateModule.forRoot()],
        });
    });

    it('renders macro state and CSS progress variables', () => {
        const fixture = createComponent();
        const element = getElement(fixture);
        const slider = getRequiredElement(element, '.goals__macro-slider');

        expect(element.textContent).toContain('GOALS_PAGE.MACROS.PROTEIN');
        expect((getRequiredElement(element, '.goals__macro-input') as HTMLInputElement).value).toBe(MACRO_VALUE.toString());
        expect(slider.style.getPropertyValue('--macro-progress')).toBe('55%');
        expect(slider.style.getPropertyValue('--macro-progress-ratio')).toBe(MACRO_RATIO.toString());
        expect(slider.style.getPropertyValue('--macro-accent')).toBe('var(--fd-color-green-500)');
    });

    it('emits value and slider input events', () => {
        const fixture = createComponent();
        const valueInput = vi.fn();
        const sliderInput = vi.fn();
        fixture.componentInstance.valueInput.subscribe(valueInput);
        fixture.componentInstance.sliderInput.subscribe(sliderInput);
        const element = getElement(fixture);

        getRequiredElement(element, '.goals__macro-input').dispatchEvent(new Event('input'));
        getRequiredElement(element, '.goals__macro-range').dispatchEvent(new Event('input'));

        expect(valueInput).toHaveBeenCalledTimes(1);
        expect(sliderInput).toHaveBeenCalledTimes(1);
    });
});

function createComponent(overrides: Partial<MacroSliderView> = {}): ComponentFixture<GoalsMacroSliderComponent> {
    const fixture = TestBed.createComponent(GoalsMacroSliderComponent);
    fixture.componentRef.setInput('macro', {
        key: 'protein',
        labelKey: 'GOALS_PAGE.MACROS.PROTEIN',
        unit: 'g',
        max: MACRO_MAX,
        value: MACRO_VALUE,
        accent: 'var(--fd-color-green-500)',
        gradient: 'linear-gradient(90deg, green, red)',
        progressOffset: '55%',
        progressRatio: MACRO_RATIO,
        ...overrides,
    });
    fixture.detectChanges();

    return fixture;
}

function getElement(fixture: ComponentFixture<GoalsMacroSliderComponent>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}

function getRequiredElement(element: HTMLElement, selector: string): HTMLElement {
    const target = element.querySelector<HTMLElement>(selector);
    if (target === null) {
        throw new Error(`Element "${selector}" was not found.`);
    }

    return target;
}
