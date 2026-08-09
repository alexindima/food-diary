import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import type { Badge } from '../../../models/gamification.data';
import { GamificationHabitPathComponent } from './gamification-habit-path';

const BADGES: Badge[] = [
    { key: 'streak_3', category: 'streak', threshold: 3, isEarned: true },
    { key: 'streak_7', category: 'streak', threshold: 7, isEarned: false },
    { key: 'meals_10', category: 'meals', threshold: 10, isEarned: true },
    { key: 'meals_50', category: 'meals', threshold: 50, isEarned: false },
];
const TOTAL_MEALS_LOGGED = 37;
const HEALTH_SCORE = 9;

describe('GamificationHabitPathComponent', () => {
    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [GamificationHabitPathComponent],
            providers: [provideTranslateTesting()],
        });
    });

    it('renders real progress and preserves earned and upcoming rewards', () => {
        const fixture = createComponent();
        const element = getElement(fixture);

        expect(element.textContent).toContain('37 / 50');
        expect(element.textContent).toContain('1 / 7');
        expect(element.querySelectorAll('.habit-path__recent-item')).toHaveLength(2);
        expect(element.querySelectorAll('.habit-path__badge')).toHaveLength(BADGES.length);
    });

    it('filters the collection by nutrition rewards', () => {
        const fixture = createComponent();
        const element = getElement(fixture);
        const nutritionFilter = Array.from(element.querySelectorAll<HTMLButtonElement>('.habit-path__filters button')).find(button =>
            button.textContent.includes('GAMIFICATION.HABIT_PATH.FILTER_NUTRITION'),
        );

        nutritionFilter?.click();
        fixture.detectChanges();

        expect(element.querySelectorAll('.habit-path__badge')).toHaveLength(2);
    });
});

function createComponent(): ComponentFixture<GamificationHabitPathComponent> {
    const fixture = TestBed.createComponent(GamificationHabitPathComponent);
    fixture.componentRef.setInput('currentStreak', 1);
    fixture.componentRef.setInput('totalMealsLogged', TOTAL_MEALS_LOGGED);
    fixture.componentRef.setInput('healthScore', HEALTH_SCORE);
    fixture.componentRef.setInput('weeklyAdherence', 0);
    fixture.componentRef.setInput('badges', BADGES);
    fixture.detectChanges();
    return fixture;
}

function getElement(fixture: ComponentFixture<GamificationHabitPathComponent>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}
