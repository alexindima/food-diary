import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it } from 'vitest';

import { USDA_NUTRIENT_IDS } from '../../lib/usda-nutrient.constants';
import type { Micronutrient } from '../../models/usda.data';
import { MicronutrientPanelComponent } from './micronutrient-panel.component';

const VITAMIN_C_AMOUNT = 24;
const CALCIUM_AMOUNT = 120;
const ENERGY_AMOUNT = 50;

describe('MicronutrientPanelComponent', () => {
    it('renders empty state when there are no vitamin or mineral nutrients', () => {
        const { fixture } = setupComponent([]);

        expect(getText(fixture)).toContain('MICRONUTRIENTS.NO_DATA');
    });

    it('splits nutrients into vitamins and minerals', () => {
        const { component, fixture } = setupComponent([
            createMicronutrient(USDA_NUTRIENT_IDS.vitaminC, 'Vitamin C', 'mg', VITAMIN_C_AMOUNT),
            createMicronutrient(USDA_NUTRIENT_IDS.calcium, 'Calcium', 'mg', CALCIUM_AMOUNT),
            createMicronutrient(USDA_NUTRIENT_IDS.energy, 'Energy', 'kcal', ENERGY_AMOUNT),
        ]);
        const text = getText(fixture);

        expect(component.vitamins().map(nutrient => nutrient.name)).toEqual(['Vitamin C']);
        expect(component.minerals().map(nutrient => nutrient.name)).toEqual(['Calcium']);
        expect(text).toContain('Vitamin C');
        expect(text).toContain('Calcium');
    });
});

function setupComponent(nutrients: Micronutrient[]): {
    component: MicronutrientPanelComponent;
    fixture: ComponentFixture<MicronutrientPanelComponent>;
} {
    TestBed.configureTestingModule({
        imports: [MicronutrientPanelComponent, TranslateModule.forRoot()],
    });

    const fixture = TestBed.createComponent(MicronutrientPanelComponent);
    fixture.componentRef.setInput('nutrients', nutrients);
    fixture.detectChanges();

    return {
        component: fixture.componentInstance,
        fixture,
    };
}

function createMicronutrient(nutrientId: number, name: string, unit: string, amountPer100g: number): Micronutrient {
    return {
        nutrientId,
        name,
        unit,
        amountPer100g,
        dailyValue: null,
        percentDailyValue: null,
    };
}

function getText(fixture: ComponentFixture<MicronutrientPanelComponent>): string {
    return (fixture.nativeElement as HTMLElement).textContent;
}
