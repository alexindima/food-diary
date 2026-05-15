import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import type { MacroSliderView } from '../../goals-page-lib/goals-page.models';
import { GoalsFiberCardComponent } from './goals-fiber-card.component';

const FIBER_VALUE = 30;

describe('GoalsFiberCardComponent', () => {
    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [GoalsFiberCardComponent, TranslateModule.forRoot()],
        });
    });

    it('renders nothing without fiber state', () => {
        const fixture = createComponent(null);

        expect(getElement(fixture).querySelector('fd-ui-card')).toBeNull();
    });

    it('renders fiber slider and emits macro events', () => {
        const fixture = createComponent(createFiber());
        const macroInput = vi.fn();
        const macroSlider = vi.fn();
        fixture.componentInstance.macroInput.subscribe(macroInput);
        fixture.componentInstance.macroSlider.subscribe(macroSlider);
        const element = getElement(fixture);

        expect(element.textContent).toContain('GOALS_PAGE.FIBER_GROUP');
        element.querySelector<HTMLInputElement>('.goals__macro-input')?.dispatchEvent(new Event('input'));
        element.querySelector<HTMLInputElement>('.goals__macro-range')?.dispatchEvent(new Event('input'));

        expect(macroInput).toHaveBeenCalledWith(expect.objectContaining({ key: 'fiber' }));
        expect(macroSlider).toHaveBeenCalledWith(expect.objectContaining({ key: 'fiber' }));
    });
});

function createComponent(fiber: MacroSliderView | null): ComponentFixture<GoalsFiberCardComponent> {
    const fixture = TestBed.createComponent(GoalsFiberCardComponent);
    fixture.componentRef.setInput('fiber', fiber);
    fixture.detectChanges();

    return fixture;
}

function getElement(fixture: ComponentFixture<GoalsFiberCardComponent>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}

function createFiber(): MacroSliderView {
    return {
        key: 'fiber',
        labelKey: 'GOALS_PAGE.MACROS.FIBER',
        unit: 'g',
        max: 80,
        value: FIBER_VALUE,
        accent: 'var(--fd-color-green-500)',
        gradient: 'linear-gradient(90deg, green, red)',
        progressOffset: '37%',
        progressRatio: 0.37,
    };
}
