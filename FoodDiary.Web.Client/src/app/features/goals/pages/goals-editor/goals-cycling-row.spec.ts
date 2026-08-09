import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { beforeEach, describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import type { DayCalorieKey } from '../../models/goals.data';
import { GoalsCyclingDayComponent } from './goals-cycling-day';
import { GoalsCyclingRowComponent } from './goals-cycling-row';

const BASE_CALORIES = 2200;
const CHANGED_WEDNESDAY_CALORIES = 2600;
const EXPECTED_TOTAL = 15_800;
const EXPECTED_AVERAGE = 2257;
const DAYS_PER_WEEK = 7;
const AVERAGE_LINE_PERCENT = 50;

describe('GoalsCyclingRowComponent', () => {
    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [GoalsCyclingRowComponent],
            providers: [provideTranslateTesting()],
        }).compileComponents();
    });

    it('renders seven editable days and calculated weekly totals when enabled', () => {
        const fixture = createComponent(createDayCalories(CHANGED_WEDNESDAY_CALORIES));
        const compactText = (fixture.nativeElement as HTMLElement).textContent.replaceAll(/[,\s]/g, '');

        expect(fixture.debugElement.queryAll(By.directive(GoalsCyclingDayComponent))).toHaveLength(DAYS_PER_WEEK);
        expect(compactText).toContain(EXPECTED_TOTAL.toString());
        expect(compactText).toContain(EXPECTED_AVERAGE.toString());
    });

    it('emits the base target for every day when reset is selected', () => {
        const fixture = createComponent(createDayCalories(CHANGED_WEDNESDAY_CALORIES));
        const changes: Array<{ key: DayCalorieKey; value: number }> = [];
        fixture.componentInstance.dayCaloriesChange.subscribe(change => changes.push(change));

        fixture.debugElement.queryAll(By.css('fd-ui-button'))[1].triggerEventHandler('click');

        expect(changes).toHaveLength(DAYS_PER_WEEK);
        expect(changes.every(change => change.value === BASE_CALORIES)).toBe(true);
    });

    it('fills every bar halfway when all days equal the weekly average', () => {
        const fixture = createComponent(createDayCalories(BASE_CALORIES));
        const days = fixture.debugElement
            .queryAll(By.directive(GoalsCyclingDayComponent))
            .map(debugElement => debugElement.componentInstance as GoalsCyclingDayComponent);

        expect(days.every(day => day.barPercent() === AVERAGE_LINE_PERCENT)).toBe(true);
    });
});

function createComponent(dayCalories: Record<DayCalorieKey, number>): ComponentFixture<GoalsCyclingRowComponent> {
    const fixture = TestBed.createComponent(GoalsCyclingRowComponent);
    fixture.componentRef.setInput('enabled', true);
    fixture.componentRef.setInput('baseCalories', BASE_CALORIES);
    fixture.componentRef.setInput('dayCalories', dayCalories);
    fixture.detectChanges();
    return fixture;
}

function createDayCalories(wednesdayCalories: number): Record<DayCalorieKey, number> {
    return {
        mondayCalories: BASE_CALORIES,
        tuesdayCalories: BASE_CALORIES,
        wednesdayCalories,
        thursdayCalories: BASE_CALORIES,
        fridayCalories: BASE_CALORIES,
        saturdayCalories: BASE_CALORIES,
        sundayCalories: BASE_CALORIES,
    };
}
