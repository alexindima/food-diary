import { registerLocaleData } from '@angular/common';
import localeRu from '@angular/common/locales/ru';
import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
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
const STREAK_DAYS = 2;
const LONGER_STREAK_BADGE: Badge = { key: 'streak_14', category: 'streak', threshold: 14, isEarned: false };
const openDialog = vi.fn<(...args: DialogOpenCall) => unknown>();

beforeEach(() => {
    openDialog.mockReset();
    TestBed.configureTestingModule({
        imports: [GamificationHabitPathComponent],
        providers: [provideTranslateTesting(), { provide: FdUiDialogService, useValue: { open: openDialog } }],
    });
});

describe('GamificationHabitPathComponent', () => {
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

    /* eslint-disable @typescript-eslint/no-magic-numbers -- These counts cover English and Russian plural boundaries. */
    it.each([
        ['en', 1, 'ONE'],
        ['en', 21, 'OTHER'],
        ['ru', 1, 'ONE'],
        ['ru', 2, 'FEW'],
        ['ru', 11, 'MANY'],
        ['ru', 21, 'ONE'],
    ])('uses %s plural form for %i days', (language, count, category) => {
        TestBed.inject(TranslateService).use(String(language));
        const fixture = createComponent();
        fixture.componentRef.setInput('currentStreak', Number(count));
        fixture.detectChanges();
        expect(getElement(fixture).textContent).toContain(`GAMIFICATION.HABIT_PATH.DAYS_IN_A_ROW_${category}`);
    });
    /* eslint-enable @typescript-eslint/no-magic-numbers -- Restore the normal rule after boundary cases. */

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

describe('Mounted gamification progress', () => {
    it('updates the earned badge date and streak label when switching languages', () => {
        registerLocaleData(localeRu);
        const translate = TestBed.inject(TranslateService);
        translate.use('en');
        const fixture = createComponent();
        fixture.componentRef.setInput('currentStreak', STREAK_DAYS);
        fixture.detectChanges();
        const element = getElement(fixture);
        const earnedDate = element.querySelector('.habit-path__recent-item time');

        expect(element.querySelector('.habit-path__streak-value')?.textContent).toContain('GAMIFICATION.HABIT_PATH.DAYS_IN_A_ROW_OTHER');
        expect(earnedDate?.textContent).toContain('Aug');

        translate.use('ru');
        fixture.detectChanges();

        expect(element.querySelector('.habit-path__streak-value')?.textContent).toContain('GAMIFICATION.HABIT_PATH.DAYS_IN_A_ROW_FEW');
        expect(element.querySelector('.habit-path__recent-item time')).toBe(earnedDate);
        expect(earnedDate?.getAttribute('datetime')).toBe('2026-08-09T12:00:00Z');
        expect(earnedDate?.textContent).toContain('авг');
    });

    it('shows the nearest locked streak goal when a farther reward appears first', () => {
        const fixture = createComponent();
        fixture.componentRef.setInput('badges', [LONGER_STREAK_BADGE, ...BADGES]);
        fixture.detectChanges();

        const nextStreak = getElement(fixture).querySelector('.habit-path__goal');
        expect(nextStreak?.textContent).toContain('GAMIFICATION.BADGE_STREAK_7');
        expect(nextStreak?.textContent).toContain('/ 7');
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
