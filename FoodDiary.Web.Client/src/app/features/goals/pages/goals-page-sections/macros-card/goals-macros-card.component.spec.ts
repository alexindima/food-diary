import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import type { MacroSliderView } from '../../goals-page-lib/goals-page.models';
import { GoalsMacrosCardComponent } from './goals-macros-card.component';

const PROTEIN_VALUE = 150;
const FATS_VALUE = 70;

describe('GoalsMacrosCardComponent', () => {
    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [GoalsMacrosCardComponent, TranslateModule.forRoot()],
        });
    });

    it('renders preset selector and macro sliders', () => {
        const fixture = createComponent();
        const element = getElement(fixture);

        expect(element.textContent).toContain('GOALS_PAGE.CORE_MACROS');
        expect(element.querySelector('fd-ui-select')).not.toBeNull();
        expect(element.querySelectorAll('fd-goals-macro-slider')).toHaveLength(2);
        expect(element.querySelector<HTMLInputElement>('.goals__macro-input')?.value).toBe(PROTEIN_VALUE.toString());
    });

    it('emits macro input and slider changes with macro key', () => {
        const fixture = createComponent();
        const macroInput = vi.fn();
        const macroSlider = vi.fn();
        fixture.componentInstance.macroInput.subscribe(macroInput);
        fixture.componentInstance.macroSlider.subscribe(macroSlider);
        const element = getElement(fixture);

        element.querySelector<HTMLInputElement>('.goals__macro-input')?.dispatchEvent(new Event('input'));
        element.querySelector<HTMLInputElement>('.goals__macro-range')?.dispatchEvent(new Event('input'));

        expect(macroInput).toHaveBeenCalledWith(expect.objectContaining({ key: 'protein' }));
        expect(macroSlider).toHaveBeenCalledWith(expect.objectContaining({ key: 'protein' }));
    });
});

function createComponent(): ComponentFixture<GoalsMacrosCardComponent> {
    const fixture = TestBed.createComponent(GoalsMacrosCardComponent);
    fixture.componentRef.setInput('macroPresetOptions', [
        { value: 'custom', label: 'Custom' },
        { value: 'classic', label: 'Classic' },
    ]);
    fixture.componentRef.setInput('selectedPreset', 'custom');
    fixture.componentRef.setInput('macros', createMacros());
    fixture.detectChanges();

    return fixture;
}

function getElement(fixture: ComponentFixture<GoalsMacrosCardComponent>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}

function createMacros(): MacroSliderView[] {
    return [
        {
            key: 'protein',
            labelKey: 'GOALS_PAGE.MACROS.PROTEIN',
            unit: 'g',
            max: 220,
            value: PROTEIN_VALUE,
            accent: 'var(--fd-color-green-500)',
            gradient: 'linear-gradient(90deg, green, red)',
            progressOffset: '68%',
            progressRatio: 0.68,
        },
        {
            key: 'fats',
            labelKey: 'GOALS_PAGE.MACROS.FATS',
            unit: 'g',
            max: 160,
            value: FATS_VALUE,
            accent: 'var(--fd-color-green-500)',
            gradient: 'linear-gradient(90deg, green, red)',
            progressOffset: '44%',
            progressRatio: 0.44,
        },
    ];
}
