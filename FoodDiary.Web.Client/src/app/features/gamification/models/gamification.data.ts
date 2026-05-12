export type GamificationData = {
    currentStreak: number;
    longestStreak: number;
    totalMealsLogged: number;
    healthScore: number;
    weeklyAdherence: number;
    badges: Badge[];
};

export type Badge = {
    key: string;
    category: string;
    threshold: number;
    isEarned: boolean;
};
