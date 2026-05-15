import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import type { MealPlanDetailHeaderViewModel } from '../../../../lib/meal-plan-view.mapper';
import { MealPlanDetailHeaderComponent } from './meal-plan-detail-header.component';

describe('MealPlanDetailHeaderComponent', () => {
    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [MealPlanDetailHeaderComponent, TranslateModule.forRoot()],
        });
    });

    it('renders adopt action only for curated plans', () => {
        const curatedFixture = createComponent({ isCurated: true });
        const customFixture = createComponent({ isCurated: false });

        expect(getElement(curatedFixture).querySelectorAll('fd-ui-button')).toHaveLength(2);
        expect(getElement(customFixture).querySelectorAll('fd-ui-button')).toHaveLength(1);
    });

    it('emits header actions', () => {
        const fixture = createComponent({ isCurated: true });
        const adoptPlan = vi.fn();
        const generateShoppingList = vi.fn();
        fixture.componentInstance.adoptPlan.subscribe(adoptPlan);
        fixture.componentInstance.generateShoppingList.subscribe(generateShoppingList);
        const buttons = getElement(fixture).querySelectorAll<HTMLElement>('fd-ui-button');

        buttons[0].click();
        buttons[1].click();

        expect(adoptPlan).toHaveBeenCalled();
        expect(generateShoppingList).toHaveBeenCalled();
    });
});

function createComponent(overrides: Partial<MealPlanDetailHeaderViewModel> = {}): ComponentFixture<MealPlanDetailHeaderComponent> {
    const fixture = TestBed.createComponent(MealPlanDetailHeaderComponent);
    fixture.componentRef.setInput('plan', {
        dietTypeKey: 'MEAL_PLANS.DIET_TYPE.KETO',
        name: 'Keto plan',
        description: null,
        isCurated: true,
        ...overrides,
    });
    fixture.detectChanges();

    return fixture;
}

function getElement(fixture: ComponentFixture<MealPlanDetailHeaderComponent>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}
