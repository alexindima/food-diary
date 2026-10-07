import { normalizeMealType } from '../../lib/meal-type.util';
import { normalizeSatietyLevel } from '../../lib/satiety-level.utils';
import {
    createEmptyProductSnapshot,
    createEmptyRecipeSnapshot,
    type Meal,
    type MealAiSession,
    type MealAiSessionResponseDto,
    type MealItem,
    type MealItemResponseDto,
    type MealManageDto,
    type MealResponseDto,
    MealSourceType,
} from '../../models/meal.data';
import { MeasurementUnit, type Product } from '../../models/product.data';
import type { Recipe } from '../../models/recipe.data';
import type { CreateMealHttpRequest } from './generated/model/create-meal-http-request';
import type { MealHttpResponse } from './generated/model/meal-http-response';
import { requireSdkFields, sdkEnum, sdkOptional } from './sdk-response';

const MEAL_API_NUTRITION_CLOSE_TOLERANCE = 0.000001;
const MEAL_API_DEFAULT_ITEM_AMOUNT = 1;
const MEAL_API_EMPTY_NUTRITION_VALUE = 0;
const MEAL_NUTRITION_FIELDS = ['calories', 'proteins', 'fats', 'carbs', 'fiber', 'alcohol'] as const;
type NutritionField = (typeof MEAL_NUTRITION_FIELDS)[number];
type NutritionTotals = Record<NutritionField, number>;

/** Keep snapshots and precision intact before the existing meal normalization. */
export function mealResponseFromSdk(response: MealHttpResponse): MealResponseDto {
    const value = requireSdkFields(response, [
        'id',
        'date',
        'totalCalories',
        'totalProteins',
        'totalFats',
        'totalCarbs',
        'totalFiber',
        'totalAlcohol',
        'isNutritionAutoCalculated',
        'items',
    ]);
    return {
        ...value,
        qualityGrade: sdkOptional(value.qualityGrade, grade => sdkEnum(grade, ['green', 'yellow', 'red'] as const)),
        items: value.items.map(item => requireSdkFields(item, ['id', 'mealId', 'amount'])),
        aiSessions: value.aiSessions?.map(sessionResponse => {
            const session = requireSdkFields(sessionResponse, ['id', 'mealId', 'recognizedAtUtc', 'items']);
            return {
                ...session,
                items: session.items.map(item =>
                    requireSdkFields(item, [
                        'id',
                        'sessionId',
                        'nameEn',
                        'amount',
                        'unit',
                        'calories',
                        'proteins',
                        'fats',
                        'carbs',
                        'fiber',
                        'alcohol',
                    ]),
                ),
            };
        }),
    };
}

export function mealRequestToSdk(data: MealManageDto): CreateMealHttpRequest {
    return {
        ...data,
        date: data.date.toISOString(),
        preMealSatietyLevel: normalizeSatietyLevel(data.preMealSatietyLevel) ?? 0,
        postMealSatietyLevel: normalizeSatietyLevel(data.postMealSatietyLevel) ?? 0,
    };
}

class MealResponseNormalizer {
    public mapMeal(response: MealResponseDto): Meal {
        const isNutritionAutoCalculated = this.resolveIsNutritionAutoCalculated(response);

        return {
            id: response.id,
            date: response.date,
            mealType: normalizeMealType(response.mealType),
            comment: response.comment,
            imageUrl: this.toNullable(response.imageUrl),
            imageAssetId: this.toNullable(response.imageAssetId),
            totalCalories: response.totalCalories,
            totalProteins: response.totalProteins,
            totalFats: response.totalFats,
            totalCarbs: response.totalCarbs,
            totalFiber: response.totalFiber,
            totalAlcohol: response.totalAlcohol,
            isNutritionAutoCalculated,
            manualCalories: this.toNullable(response.manualCalories),
            manualProteins: this.toNullable(response.manualProteins),
            manualFats: this.toNullable(response.manualFats),
            manualCarbs: this.toNullable(response.manualCarbs),
            manualFiber: this.toNullable(response.manualFiber),
            manualAlcohol: this.toNullable(response.manualAlcohol),
            preMealSatietyLevel: normalizeSatietyLevel(response.preMealSatietyLevel),
            postMealSatietyLevel: normalizeSatietyLevel(response.postMealSatietyLevel),
            qualityScore: this.toNullable(response.qualityScore),
            qualityGrade: this.toNullable(response.qualityGrade),
            isFavorite: this.withDefault(response.isFavorite, false),
            favoriteMealId: this.toNullable(response.favoriteMealId),
            items: response.items.map(item => this.mapMealItem(item)),
            aiSessions: this.mapOptionalArray(response.aiSessions, session => this.mapAiSession(session)),
        };
    }

    private resolveIsNutritionAutoCalculated(response: MealResponseDto): boolean {
        const isAuto = response.isNutritionAutoCalculated;
        if (this.shouldUseServerNutritionAutoCalculated(response, isAuto)) {
            return isAuto;
        }

        const aiTotals = this.calculateAiTotals(response);
        return MEAL_NUTRITION_FIELDS.every(field => this.areClose(this.resolveResponseNutrition(response, field), aiTotals[field]));
    }

    private shouldUseServerNutritionAutoCalculated(response: MealResponseDto, isAuto: boolean): boolean {
        return isAuto || response.items.length > 0 || !this.hasAiItems(response);
    }

    private resolveResponseNutrition(response: MealResponseDto, field: NutritionField): number {
        const nutrition: NutritionTotals = {
            calories: response.manualCalories ?? response.totalCalories,
            proteins: response.manualProteins ?? response.totalProteins,
            fats: response.manualFats ?? response.totalFats,
            carbs: response.manualCarbs ?? response.totalCarbs,
            fiber: response.manualFiber ?? response.totalFiber,
            alcohol: response.manualAlcohol ?? response.totalAlcohol,
        };

        return nutrition[field];
    }

    private hasAiItems(response: MealResponseDto): boolean {
        return response.aiSessions?.some(session => session.items.length > 0) ?? false;
    }

    private calculateAiTotals(response: MealResponseDto): NutritionTotals {
        return (
            response.aiSessions?.reduce(
                (totals, session) =>
                    session.items.reduce(
                        (sessionTotals, item) => ({
                            calories: sessionTotals.calories + item.calories,
                            proteins: sessionTotals.proteins + item.proteins,
                            fats: sessionTotals.fats + item.fats,
                            carbs: sessionTotals.carbs + item.carbs,
                            fiber: sessionTotals.fiber + item.fiber,
                            alcohol: sessionTotals.alcohol + item.alcohol,
                        }),
                        totals,
                    ),
                this.createEmptyNutritionTotals(),
            ) ?? this.createEmptyNutritionTotals()
        );
    }

    private areClose(left: number, right: number): boolean {
        return Math.abs(left - right) <= MEAL_API_NUTRITION_CLOSE_TOLERANCE;
    }

    private mapMealItem(response: MealItemResponseDto): MealItem {
        const product =
            response.productId !== null && response.productId !== undefined && response.productId.length > 0
                ? this.createProductFromSnapshot(response)
                : null;
        const recipe =
            response.recipeId !== null && response.recipeId !== undefined && response.recipeId.length > 0
                ? this.createRecipeFromSnapshot(response)
                : null;
        const sourceType = product !== null ? MealSourceType.Product : MealSourceType.Recipe;

        return {
            id: response.id,
            mealId: response.mealId,
            amount: response.amount,
            sourceType,
            sourceAiItemId: response.sourceAiItemId ?? null,
            origin: response.origin ?? null,
            product,
            recipe,
        };
    }

    private createProductFromSnapshot(response: MealItemResponseDto): Product {
        const base = createEmptyProductSnapshot();
        return {
            ...base,
            id: this.withDefault(response.productId, ''),
            name: this.withDefault(response.productName, ''),
            imageUrl: this.toNullable(response.productImageUrl),
            baseUnit: this.normalizeMeasurementUnit(response.productBaseUnit),
            baseAmount: this.withDefault(response.productBaseAmount, MEAL_API_DEFAULT_ITEM_AMOUNT),
            defaultPortionAmount: this.withDefault(response.productBaseAmount, MEAL_API_DEFAULT_ITEM_AMOUNT),
            caloriesPerBase: this.withDefault(response.productCaloriesPerBase, MEAL_API_EMPTY_NUTRITION_VALUE),
            proteinsPerBase: this.withDefault(response.productProteinsPerBase, MEAL_API_EMPTY_NUTRITION_VALUE),
            fatsPerBase: this.withDefault(response.productFatsPerBase, MEAL_API_EMPTY_NUTRITION_VALUE),
            carbsPerBase: this.withDefault(response.productCarbsPerBase, MEAL_API_EMPTY_NUTRITION_VALUE),
            fiberPerBase: this.withDefault(response.productFiberPerBase, MEAL_API_EMPTY_NUTRITION_VALUE),
            alcoholPerBase: this.withDefault(response.productAlcoholPerBase, MEAL_API_EMPTY_NUTRITION_VALUE),
        };
    }

    private createRecipeFromSnapshot(response: MealItemResponseDto): Recipe {
        const base = createEmptyRecipeSnapshot();
        return {
            ...base,
            id: this.withDefault(response.recipeId, ''),
            name: this.withDefault(response.recipeName, ''),
            imageUrl: this.toNullable(response.recipeImageUrl),
            servings: this.withDefault(response.recipeServings, MEAL_API_DEFAULT_ITEM_AMOUNT),
            totalCalories: this.withDefault(response.recipeTotalCalories, MEAL_API_EMPTY_NUTRITION_VALUE),
            totalProteins: this.withDefault(response.recipeTotalProteins, MEAL_API_EMPTY_NUTRITION_VALUE),
            totalFats: this.withDefault(response.recipeTotalFats, MEAL_API_EMPTY_NUTRITION_VALUE),
            totalCarbs: this.withDefault(response.recipeTotalCarbs, MEAL_API_EMPTY_NUTRITION_VALUE),
            totalFiber: this.withDefault(response.recipeTotalFiber, MEAL_API_EMPTY_NUTRITION_VALUE),
            totalAlcohol: this.withDefault(response.recipeTotalAlcohol, MEAL_API_EMPTY_NUTRITION_VALUE),
        };
    }

    private mapAiSession(response: MealAiSessionResponseDto): MealAiSession {
        return {
            id: response.id,
            mealId: response.mealId,
            imageAssetId: response.imageAssetId ?? null,
            imageUrl: response.imageUrl ?? null,
            status: response.status ?? null,
            recognizedAtUtc: response.recognizedAtUtc,
            notes: response.notes ?? null,
            items: response.items.map(item => ({
                id: item.id,
                sessionId: item.sessionId,
                nameEn: item.nameEn,
                nameLocal: item.nameLocal ?? null,
                amount: item.amount,
                unit: item.unit,
                calories: item.calories,
                proteins: item.proteins,
                fats: item.fats,
                carbs: item.carbs,
                fiber: item.fiber,
                alcohol: item.alcohol,
                confidence: item.confidence ?? 1,
                resolution: item.resolution ?? 'Accepted',
            })),
        };
    }

    private normalizeMeasurementUnit(unit?: MeasurementUnit | string | null): MeasurementUnit {
        if (unit === null || unit === undefined || unit.length === 0) {
            return MeasurementUnit.G;
        }

        const normalized = unit.toString().toUpperCase();
        if (this.isMeasurementUnit(normalized)) {
            return normalized;
        }

        return MeasurementUnit.G;
    }

    private isMeasurementUnit(value: string): value is MeasurementUnit {
        return value === 'G' || value === 'ML' || value === 'PCS';
    }

    private createEmptyNutritionTotals(): NutritionTotals {
        return {
            calories: MEAL_API_EMPTY_NUTRITION_VALUE,
            proteins: MEAL_API_EMPTY_NUTRITION_VALUE,
            fats: MEAL_API_EMPTY_NUTRITION_VALUE,
            carbs: MEAL_API_EMPTY_NUTRITION_VALUE,
            fiber: MEAL_API_EMPTY_NUTRITION_VALUE,
            alcohol: MEAL_API_EMPTY_NUTRITION_VALUE,
        };
    }

    private toNullable<T>(value: T | null | undefined): T | null {
        return value ?? null;
    }

    private withDefault<T>(value: T | null | undefined, fallback: T): T {
        return value ?? fallback;
    }

    private mapOptionalArray<T, R>(items: T[] | null | undefined, mapper: (item: T) => R): R[] {
        return items?.map(mapper) ?? [];
    }
}

const normalizer = new MealResponseNormalizer();

export function mealFromSdk(response: MealHttpResponse): Meal {
    return normalizer.mapMeal(mealResponseFromSdk(response));
}
