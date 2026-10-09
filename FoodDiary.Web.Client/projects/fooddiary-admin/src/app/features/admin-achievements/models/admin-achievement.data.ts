import type { AdminId } from '../../../shared/models/semantics/admin-meaning';
export type AchievementMetric = 'LongestStreak' | 'TotalMeals' | 'TotalAcademyArticlesRead';

export type AdminAchievementDefinition = {
    id: AdminId<'achievement-definition'>;
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
    awardedUsers?: number;
};

export type CreateAdminAchievementDefinitionRequest = Omit<AdminAchievementDefinition, 'id' | 'version'>;
export type UpdateAdminAchievementDefinitionRequest = Omit<AdminAchievementDefinition, 'id' | 'key'>;
