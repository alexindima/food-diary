import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { GamificationService } from '../api/gamification.service';
import type { GamificationData } from '../models/gamification.data';
import { GamificationFacade } from './gamification.facade';

const CURRENT_STREAK = 4;
const LONGEST_STREAK = 15;
const TOTAL_MEALS_LOGGED = 72;
const HEALTH_SCORE = 81;
const WEEKLY_ADHERENCE = 0.875;
const WEEKLY_ADHERENCE_PERCENT = 88;
const WAIT_ATTEMPTS = 20;
const MOCK_DATA: GamificationData = {
    currentStreak: CURRENT_STREAK,
    longestStreak: LONGEST_STREAK,
    totalMealsLogged: TOTAL_MEALS_LOGGED,
    healthScore: HEALTH_SCORE,
    weeklyAdherence: WEEKLY_ADHERENCE,
    badges: [{ key: 'streak_3', category: 'streak', threshold: 3, isEarned: true }],
};

let facade: GamificationFacade;
let gamificationService: { getData: ReturnType<typeof vi.fn> };

beforeEach(() => {
    gamificationService = {
        getData: vi.fn().mockReturnValue(of(MOCK_DATA)),
    };

    TestBed.configureTestingModule({
        providers: [GamificationFacade, { provide: GamificationService, useValue: gamificationService }],
    });

    facade = TestBed.inject(GamificationFacade);
});

describe('GamificationFacade', () => {
    it('exposes default values before data is loaded', () => {
        expect(facade.data()).toBeNull();
        expect(facade.currentStreak()).toBe(0);
        expect(facade.longestStreak()).toBe(0);
        expect(facade.totalMealsLogged()).toBe(0);
        expect(facade.healthScore()).toBe(0);
        expect(facade.weeklyAdherence()).toBe(0);
        expect(facade.badges()).toEqual([]);
    });

    it('loads gamification data and maps adherence to percent', async () => {
        facade.initialize();
        await waitForAsync(() => facade.data() !== null);

        expect(gamificationService.getData).toHaveBeenCalled();
        expect(facade.data()).toEqual(MOCK_DATA);
        expect(facade.currentStreak()).toBe(CURRENT_STREAK);
        expect(facade.longestStreak()).toBe(LONGEST_STREAK);
        expect(facade.totalMealsLogged()).toBe(TOTAL_MEALS_LOGGED);
        expect(facade.healthScore()).toBe(HEALTH_SCORE);
        expect(facade.weeklyAdherence()).toBe(WEEKLY_ADHERENCE_PERCENT);
        expect(facade.badges()).toEqual(MOCK_DATA.badges);
    });

    it('reloads data on each initialize call', async () => {
        facade.initialize();
        await waitForAsync(() => facade.data() !== null);
        facade.initialize();
        await waitForAsync(() => gamificationService.getData.mock.calls.length === 2);

        expect(gamificationService.getData).toHaveBeenCalledTimes(2);
    });
});

async function waitForAsync(predicate: () => boolean): Promise<void> {
    for (let attempt = 0; attempt < WAIT_ATTEMPTS; attempt++) {
        TestBed.tick();

        if (predicate()) {
            return;
        }

        await Promise.resolve();
    }
}
