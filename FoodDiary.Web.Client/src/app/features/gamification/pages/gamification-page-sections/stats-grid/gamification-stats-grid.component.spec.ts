import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { beforeEach, describe, expect, it } from 'vitest';

import { GamificationStatsGridComponent } from './gamification-stats-grid.component';

const STAT_TILE_COUNT = 4;
const CURRENT_STREAK = 3;
const LONGEST_STREAK = 12;
const TOTAL_MEALS_LOGGED = 48;
const WEEKLY_ADHERENCE = 86;

describe('GamificationStatsGridComponent', () => {
    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [GamificationStatsGridComponent, TranslateModule.forRoot()],
        });
    });

    it('renders all stat tiles from input values', () => {
        const fixture = createComponent();
        const element = getElement(fixture);

        expect(element.querySelectorAll('.gamification__stat')).toHaveLength(STAT_TILE_COUNT);
        expect(element.textContent).toContain(CURRENT_STREAK.toString());
        expect(element.textContent).toContain(LONGEST_STREAK.toString());
        expect(element.textContent).toContain(TOTAL_MEALS_LOGGED.toString());
        expect(element.textContent).toContain(`${WEEKLY_ADHERENCE}%`);
        expect(element.textContent).toContain('GAMIFICATION.CURRENT_STREAK');
        expect(element.textContent).toContain('GAMIFICATION.WEEKLY_ADHERENCE');
    });
});

function createComponent(): ComponentFixture<GamificationStatsGridComponent> {
    const fixture = TestBed.createComponent(GamificationStatsGridComponent);
    fixture.componentRef.setInput('currentStreak', CURRENT_STREAK);
    fixture.componentRef.setInput('longestStreak', LONGEST_STREAK);
    fixture.componentRef.setInput('totalMealsLogged', TOTAL_MEALS_LOGGED);
    fixture.componentRef.setInput('weeklyAdherence', WEEKLY_ADHERENCE);
    fixture.detectChanges();

    return fixture;
}

function getElement(fixture: ComponentFixture<GamificationStatsGridComponent>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}
