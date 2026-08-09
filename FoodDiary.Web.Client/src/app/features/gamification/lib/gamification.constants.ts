import type { GamificationData } from '../models/gamification.data';

export function createDefaultGamificationData(): GamificationData {
    return {
        currentStreak: 0,
        longestStreak: 0,
        totalMealsLogged: 0,
        healthScore: 0,
        weeklyAdherence: 0,
        badges: [],
    };
}
