export interface GamificationData {
    currentStreak: number;
    longestStreak: number;
    totalMealsLogged: number;
    healthScore: number;
    weeklyAdherence: number;
    badges: Badge[];
}

export interface Badge {
    key: string;
    category: string;
    threshold: number;
    isEarned: boolean;
}
