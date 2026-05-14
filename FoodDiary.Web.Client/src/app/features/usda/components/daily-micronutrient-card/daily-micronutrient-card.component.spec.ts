import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it } from 'vitest';

import { USDA_NUTRIENT_IDS } from '../../lib/usda-nutrient.constants';
import type { DailyMicronutrient } from '../../models/usda.data';
import { DailyMicronutrientCardComponent } from './daily-micronutrient-card.component';

const LINKED_COUNT = 1;
const TOTAL_COUNT = 2;
const VITAMIN_C_TOTAL = 92;
const CALCIUM_TOTAL = 120;

describe('DailyMicronutrientCardComponent', () => {
    it('renders empty state when no key nutrients exist', () => {
        const { fixture } = setupComponent([]);

        expect(getText(fixture)).toContain('MICRONUTRIENTS.NO_DAILY_DATA');
    });

    it('renders linked coverage and sorted key nutrients', () => {
        const nutrients: DailyMicronutrient[] = [
            createDailyMicronutrient(USDA_NUTRIENT_IDS.vitaminC, 'Vitamin C', 'mg', VITAMIN_C_TOTAL),
            createDailyMicronutrient(USDA_NUTRIENT_IDS.calcium, 'Calcium', 'mg', CALCIUM_TOTAL),
        ];
        const { component, fixture } = setupComponent(nutrients);
        const text = getText(fixture);

        expect(component.keyNutrients().map(nutrient => nutrient.name)).toEqual(['Calcium', 'Vitamin C']);
        expect(text).toContain(`${LINKED_COUNT}/${TOTAL_COUNT}`);
        expect(text).toContain('Calcium');
        expect(text).toContain('Vitamin C');
    });
});

function setupComponent(nutrients: DailyMicronutrient[]): {
    component: DailyMicronutrientCardComponent;
    fixture: ComponentFixture<DailyMicronutrientCardComponent>;
} {
    TestBed.configureTestingModule({
        imports: [DailyMicronutrientCardComponent, TranslateModule.forRoot()],
    });

    const fixture = TestBed.createComponent(DailyMicronutrientCardComponent);
    fixture.componentRef.setInput('nutrients', nutrients);
    fixture.componentRef.setInput('linkedCount', LINKED_COUNT);
    fixture.componentRef.setInput('totalCount', TOTAL_COUNT);
    fixture.detectChanges();

    return {
        component: fixture.componentInstance,
        fixture,
    };
}

function createDailyMicronutrient(nutrientId: number, name: string, unit: string, totalAmount: number): DailyMicronutrient {
    return {
        nutrientId,
        name,
        unit,
        totalAmount,
        dailyValue: null,
        percentDailyValue: null,
    };
}

function getText(fixture: ComponentFixture<DailyMicronutrientCardComponent>): string {
    return (fixture.nativeElement as HTMLElement).textContent;
}
