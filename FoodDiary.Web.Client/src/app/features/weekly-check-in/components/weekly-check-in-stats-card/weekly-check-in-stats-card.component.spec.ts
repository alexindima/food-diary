import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it } from 'vitest';

import type { WeekSummary } from '../../models/weekly-check-in.data';
import { WeeklyCheckInStatsCardComponent } from './weekly-check-in-stats-card.component';

describe('WeeklyCheckInStatsCardComponent', () => {
    it('renders card title without week data', () => {
        const fixture = setupComponent(undefined);

        expect(getText(fixture)).toContain('WEEKLY_CHECK_IN.THIS_WEEK');
    });

    it('renders weekly stats and optional weight', () => {
        const fixture = setupComponent(createWeekSummary());
        const text = getText(fixture);

        expect(text).toContain('WEEKLY_CHECK_IN.AVG_CALORIES');
        expect(text).toContain('2,000');
        expect(text).toContain('21');
        expect(text).toContain('7 / 7');
        expect(text).toContain('110GENERAL.UNITS.G');
        expect(text).toContain('73.5 GENERAL.UNITS.KG');
        expect(text).toContain('2000 GENERAL.UNITS.ML');
    });
});

function setupComponent(week: WeekSummary | undefined): ComponentFixture<WeeklyCheckInStatsCardComponent> {
    TestBed.configureTestingModule({
        imports: [WeeklyCheckInStatsCardComponent, TranslateModule.forRoot()],
    });

    const fixture = TestBed.createComponent(WeeklyCheckInStatsCardComponent);
    fixture.componentRef.setInput('week', week);
    fixture.detectChanges();
    return fixture;
}

function getText(fixture: ComponentFixture<WeeklyCheckInStatsCardComponent>): string {
    return (fixture.nativeElement as HTMLElement).textContent;
}

function createWeekSummary(): WeekSummary {
    return {
        totalCalories: 14000,
        avgDailyCalories: 2000,
        avgProteins: 110,
        avgFats: 70,
        avgCarbs: 210,
        mealsLogged: 21,
        daysLogged: 7,
        weightStart: 74,
        weightEnd: 73.5,
        waistStart: 82,
        waistEnd: 81,
        totalHydrationMl: 14000,
        avgDailyHydrationMl: 2000,
    };
}
