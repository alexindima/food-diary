export type AchievementMetric = 'LongestStreak' | 'TotalMeals';

export type AdminAchievementDefinition = {
    id: string;
    key: string;
    category: string;
    metric: AchievementMetric;
    threshold: number;
    titleRu: string;
    titleEn: string;
    descriptionRu: string;
    descriptionEn: string;
    icon: string;
    sortOrder: number;
    isActive: boolean;
    version: number;
};

export type CreateAdminAchievementDefinitionRequest = Omit<AdminAchievementDefinition, 'id' | 'version'>;
export type UpdateAdminAchievementDefinitionRequest = Omit<AdminAchievementDefinition, 'id' | 'key'>;
