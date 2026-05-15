import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import type { MacroSliderView } from '../../goals-page-lib/goals-page.models';
import { GoalsWaterCardComponent } from './goals-water-card.component';

const WATER_VALUE = 2200;

describe('GoalsWaterCardComponent', () => {
    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [GoalsWaterCardComponent, TranslateModule.forRoot()],
        });
    });

    it('renders water slider and emits water events', () => {
        const fixture = createComponent();
        const waterInput = vi.fn();
        const waterSlider = vi.fn();
        fixture.componentInstance.waterInput.subscribe(waterInput);
        fixture.componentInstance.waterSlider.subscribe(waterSlider);
        const element = getElement(fixture);

        expect(element.textContent).toContain('GOALS_PAGE.WATER_TITLE');
        expect(element.querySelector<HTMLInputElement>('.goals__macro-input')?.value).toBe(WATER_VALUE.toString());
        element.querySelector<HTMLInputElement>('.goals__macro-input')?.dispatchEvent(new Event('input'));
        element.querySelector<HTMLInputElement>('.goals__macro-range')?.dispatchEvent(new Event('input'));

        expect(waterInput).toHaveBeenCalledTimes(1);
        expect(waterSlider).toHaveBeenCalledTimes(1);
    });
});

function createComponent(): ComponentFixture<GoalsWaterCardComponent> {
    const fixture = TestBed.createComponent(GoalsWaterCardComponent);
    fixture.componentRef.setInput('water', createWater());
    fixture.detectChanges();

    return fixture;
}

function getElement(fixture: ComponentFixture<GoalsWaterCardComponent>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}

function createWater(): MacroSliderView {
    return {
        labelKey: 'GOALS_PAGE.WATER_LABEL',
        unit: 'ml',
        max: 4000,
        value: WATER_VALUE,
        accent: 'var(--fd-color-green-500)',
        gradient: 'linear-gradient(90deg, green, red)',
        progressOffset: '55%',
        progressRatio: 0.55,
    };
}
