import type { DailyMicronutrientSummaryHttpResponse } from '../../../shared/api/sdk/generated/model/daily-micronutrient-summary-http-response';
import type { HealthAreaScoreHttpResponse } from '../../../shared/api/sdk/generated/model/health-area-score-http-response';
import type { HealthAreaScoresHttpResponse } from '../../../shared/api/sdk/generated/model/health-area-scores-http-response';
import type { UsdaFoodDetailHttpResponse } from '../../../shared/api/sdk/generated/model/usda-food-detail-http-response';
import { requireSdkFields, sdkEnum } from '../../../shared/api/sdk/sdk-response';
import { usdaFoodId } from '../../../shared/models/semantics/usda-food-id';
import type { DailyMicronutrientSummary, HealthAreaScore, HealthAreaScores, UsdaFoodDetail } from '../../../shared/models/usda.data';

function healthScoreFromSdk(value: HealthAreaScoreHttpResponse): HealthAreaScore {
    const row = requireSdkFields(value, ['score', 'grade']);
    return { ...row, grade: sdkEnum(row.grade, ['unknown', 'low', 'fair', 'good', 'excellent'] as const) };
}

function healthScoresFromSdk(value?: HealthAreaScoresHttpResponse | null): HealthAreaScores | null {
    if (value === null || value === undefined) {
        return null;
    }
    const row = requireSdkFields(value, ['heart', 'bone', 'immune', 'energy', 'antioxidant']);
    return {
        heart: healthScoreFromSdk(row.heart),
        bone: healthScoreFromSdk(row.bone),
        immune: healthScoreFromSdk(row.immune),
        energy: healthScoreFromSdk(row.energy),
        antioxidant: healthScoreFromSdk(row.antioxidant),
    };
}

export function usdaDetailFromSdk(response: UsdaFoodDetailHttpResponse): UsdaFoodDetail {
    const value = requireSdkFields(response, ['fdcId', 'description', 'nutrients', 'portions']);
    return {
        ...value,
        fdcId: usdaFoodId(value.fdcId),
        foodCategory: value.foodCategory ?? null,
        healthScores: healthScoresFromSdk(value.healthScores),
        nutrients: value.nutrients.map(itemResponse => {
            const row = requireSdkFields(itemResponse, ['nutrientId', 'name', 'unit', 'amountPer100G']);
            const { amountPer100G, ...nutrient } = row;
            return {
                ...nutrient,
                amountPer100g: amountPer100G,
                dailyValue: row.dailyValue ?? null,
                percentDailyValue: row.percentDailyValue ?? null,
            };
        }),
        portions: value.portions.map(itemResponse => {
            const row = requireSdkFields(itemResponse, ['id', 'amount', 'measureUnitName', 'gramWeight']);
            return { ...row, portionDescription: row.portionDescription ?? null, modifier: row.modifier ?? null };
        }),
    };
}

export function dailyMicronutrientsFromSdk(response: DailyMicronutrientSummaryHttpResponse): DailyMicronutrientSummary {
    const value = requireSdkFields(response, ['date', 'linkedProductCount', 'totalProductCount', 'nutrients']);
    return {
        ...value,
        healthScores: healthScoresFromSdk(value.healthScores),
        nutrients: value.nutrients.map(itemResponse => {
            const row = requireSdkFields(itemResponse, ['nutrientId', 'name', 'unit', 'totalAmount']);
            return { ...row, dailyValue: row.dailyValue ?? null, percentDailyValue: row.percentDailyValue ?? null };
        }),
    };
}
