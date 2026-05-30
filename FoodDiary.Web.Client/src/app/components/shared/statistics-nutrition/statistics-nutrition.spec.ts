import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import { StatisticsNutritionComponent } from './statistics-nutrition';

type StatisticsNutritionTestContext = {
    component: StatisticsNutritionComponent;
    fixture: ComponentFixture<StatisticsNutritionComponent>;
};

async function setupStatisticsNutritionAsync(): Promise<StatisticsNutritionTestContext> {
    await TestBed.configureTestingModule({
        imports: [StatisticsNutritionComponent, TranslateModule.forRoot()],
    }).compileComponents();

    const fixture = TestBed.createComponent(StatisticsNutritionComponent);
    const component = fixture.componentInstance;
    fixture.componentRef.setInput('tabs', [{ value: 'calories', label: 'Calories' }]);
    fixture.componentRef.setInput('selectedTab', 'calories');
    fixture.componentRef.setInput('hasData', true);
    fixture.componentRef.setInput('caloriesTrendPoints', []);
    fixture.componentRef.setInput('nutrientTrendGroups', []);
    fixture.componentRef.setInput('nutrientPieSegments', []);
    fixture.componentRef.setInput('nutrientBarItems', []);

    return { component, fixture };
}

describe('StatisticsNutritionComponent', () => {
    it('emits selected tab changes', async () => {
        const { component, fixture } = await setupStatisticsNutritionAsync();
        const tabSpy = vi.fn();
        component['selectedTabChange'].subscribe(tabSpy);
        fixture.detectChanges();

        component['onTabChange']('distribution');

        expect(tabSpy).toHaveBeenCalledWith('distribution');
    });
});
