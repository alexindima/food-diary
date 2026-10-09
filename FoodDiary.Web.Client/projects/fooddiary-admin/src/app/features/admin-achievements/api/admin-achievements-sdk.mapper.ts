import type { AdminAchievementDefinitionHttpResponse } from '../../../shared/api/sdk/generated/model/admin-achievement-definition-http-response';
import { requireSdkFields, sdkEnum } from '../../../shared/api/sdk/sdk-response';
import { adminId } from '../../../shared/models/semantics/admin-meaning';
import type { AdminAchievementDefinition } from '../models/admin-achievement.data';

export function adminAchievementDefinitionFromSdk(response: AdminAchievementDefinitionHttpResponse): AdminAchievementDefinition {
    const value = requireSdkFields(response, [
        'id',
        'key',
        'category',
        'metric',
        'threshold',
        'titleRu',
        'titleEn',
        'descriptionRu',
        'descriptionEn',
        'icon',
        'sortOrder',
        'isActive',
        'version',
    ]);
    return {
        ...value,
        metric: sdkEnum(value.metric, ['LongestStreak', 'TotalMeals', 'TotalAcademyArticlesRead'] as const),
        id: adminId<'achievement-definition'>(value.id),
    };
}
