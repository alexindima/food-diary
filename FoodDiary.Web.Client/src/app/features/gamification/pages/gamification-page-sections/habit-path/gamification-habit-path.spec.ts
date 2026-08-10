import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import type { GamificationAchievementsDialogData } from '../../../dialogs/gamification-achievements-dialog/gamification-achievements-dialog';
import type { Badge } from '../../../models/gamification.data';
import { GamificationHabitPathComponent } from './gamification-habit-path';

type DialogOpenCall = [component: unknown, config: { data: GamificationAchievementsDialogData; preset: string }];

const BADGES: Badge[] = [
    { key: 'streak_3', category: 'streak', threshold: 3, isEarned: true, earnedAtUtc: '2026-08-08T12:00:00Z' },
    { key: 'streak_7', category: 'streak', threshold: 7, isEarned: false },
    { key: 'meals_10', category: 'meals', threshold: 10, isEarned: true, earnedAtUtc: '2026-08-09T12:00:00Z' },
    { key: 'meals_50', category: 'meals', threshold: 50, isEarned: false },
    { key: 'academy_articles_1', category: 'academy', threshold: 1, isEarned: true, earnedAtUtc: '2026-08-07T12:00:00Z', icon: 'school' },
    { key: 'academy_articles_5', category: 'academy', threshold: 5, isEarned: false, icon: 'school' },
];
const TOTAL_MEALS_LOGGED = 37;
const HEALTH_SCORE = 9;
const LONGEST_STREAK = 3;

describe('GamificationHabitPathComponent', () => {
    const openDialog = vi.fn<(...args: DialogOpenCall) => unknown>();

    beforeEach(() => {
        openDialog.mockReset();
        TestBed.configureTestingModule({
            imports: [GamificationHabitPathComponent],
            providers: [provideTranslateTesting(), { provide: FdUiDialogService, useValue: { open: openDialog } }],
        });
    });

    it('renders real progress and preserves earned and upcoming rewards', () => {
        const fixture = createComponent();
        const element = getElement(fixture);

        expect(element.textContent).toContain('37 / 50');
        expect(element.textContent).toContain('1 / 7');
        expect(element.querySelectorAll('.habit-path__recent-item')).toHaveLength(2);
        expect(element.querySelectorAll('.habit-path__recent-item time')).toHaveLength(2);
        expect(element.querySelector('.habit-path__recent-item time')?.getAttribute('datetime')).toBe('2026-08-09T12:00:00Z');
        expect(element.querySelectorAll('.habit-path__badge')).toHaveLength(BADGES.length);
    });

    it('opens all earned achievements from the recent card', () => {
        const fixture = createComponent();
        const element = getElement(fixture);
        const viewAll = Array.from(element.querySelectorAll<HTMLButtonElement>('button')).find(button =>
            button.textContent.includes('GAMIFICATION.HABIT_PATH.VIEW_ALL'),
        );

        viewAll?.click();

        expect(openDialog).toHaveBeenCalledOnce();
        const config = openDialog.mock.calls[0]?.[1];
        expect(config.preset).toBe('form');
        expect(config.data.badges.some(badge => badge.key === 'academy_articles_1')).toBe(true);
    });

    it('filters Academy rewards separately', () => {
        const fixture = createComponent();
        const element = getElement(fixture);
        const academyFilter = Array.from(element.querySelectorAll<HTMLButtonElement>('.habit-path__filters button')).find(button =>
            button.textContent.includes('GAMIFICATION.HABIT_PATH.FILTER_ACADEMY'),
        );

        academyFilter?.click();
        fixture.detectChanges();

        expect(element.querySelectorAll('.habit-path__badge')).toHaveLength(2);
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
    fixture.componentRef.setInput('longestStreak', LONGEST_STREAK);
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
