import type { GamificationData } from '../models/gamification.data';

export const HEALTH_SCORE_RING_RADIUS = 90;

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
