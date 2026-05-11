import { Injectable } from '@angular/core';
import { catchError, map, type Observable, of } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { rethrowApiError } from '../../../shared/lib/api-error.utils';
import { normalizeMealType } from '../../../shared/lib/meal-type.util';
import type { PageOf } from '../../../shared/models/page-of.data';
import { MeasurementUnit, type Product } from '../../products/models/product.data';
import type { Recipe } from '../../recipes/models/recipe.data';
import {
    type Consumption,
    type ConsumptionAiSession,
    type ConsumptionAiSessionResponseDto,
    type ConsumptionItem,
    type ConsumptionItemResponseDto,
    type ConsumptionOverview,
    type ConsumptionResponseDto,
    ConsumptionSourceType,
    createEmptyProductSnapshot,
    createEmptyRecipeSnapshot,
    type Meal,
    type MealFilters,
    type MealManageDto,
    type MealOverview,
} from '../models/meal.data';

const DEFAULT_FAVORITE_LIMIT = 10;
const DEFAULT_SATIETY_LEVEL = 3;
const MIN_SATIETY_LEVEL = 1;
const MAX_SATIETY_LEVEL = 5;
const SATIETY_NORMALIZATION_DIVISOR = 2;
const NUTRITION_CLOSE_TOLERANCE = 0.000001;
const DEFAULT_ITEM_AMOUNT = 1;
const EMPTY_NUTRITION_VALUE = 0;

@Injectable({
    providedIn: 'root',
})
export class MealService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.consumptions;

    public query(page: number, limit: number, filters: MealFilters): Observable<PageOf<Meal>> {
        const params = { page, limit, ...filters };
        return this.get<PageOf<ConsumptionResponseDto>>('', params).pipe(
            map(pageData => ({
                ...pageData,
                data: pageData.data.map(response => this.mapConsumption(response)),
            })),
            catchError(error => rethrowApiError('Query meals error', error)),
        );
    }

    public queryOverview(
        page: number,
        limit: number,
        filters: MealFilters,
        favoriteLimit = DEFAULT_FAVORITE_LIMIT,
    ): Observable<MealOverview> {
        const params = { page, limit, favoriteLimit, ...filters };
        return this.get<ConsumptionOverview>('overview', params).pipe(
            map(response => ({
                allConsumptions: {
                    ...response.allConsumptions,
                    data: response.allConsumptions.data.map(item => this.mapConsumption(item)),
                },
                favoriteItems: response.favoriteItems,
                favoriteTotalCount: response.favoriteTotalCount,
            })),
            catchError(error => rethrowApiError('Query meal overview error', error)),
        );
    }

    public getById(id: string): Observable<Meal | null> {
        return this.get<ConsumptionResponseDto>(id).pipe(
            map(response => this.mapConsumption(response)),
            catchError(() => of(null)),
        );
    }

    public create(data: MealManageDto): Observable<Meal | null> {
        return this.post<ConsumptionResponseDto>('', data).pipe(
            map(response => this.mapConsumption(response)),
            catchError(() => of(null)),
        );
    }

    public update(id: string, data: MealManageDto): Observable<Meal | null> {
        return this.patch<ConsumptionResponseDto>(id, data).pipe(
            map(response => this.mapConsumption(response)),
            catchError(() => of(null)),
        );
    }

    public deleteById(id: string): Observable<void> {
        return this.delete<void>(id).pipe(catchError(error => rethrowApiError('Delete meal error', error)));
    }

    public repeat(id: string, targetDate: string, mealType?: string): Observable<Meal> {
        return this.post<ConsumptionResponseDto>(`${id}/repeat`, { targetDate, mealType }).pipe(
            map(response => this.mapConsumption(response)),
            catchError(error => rethrowApiError('Repeat meal error', error)),
        );
    }

    private mapConsumption(response: ConsumptionResponseDto): Consumption {
        const isNutritionAutoCalculated = this.resolveIsNutritionAutoCalculated(response);

        return {
            id: response.id,
            date: response.date,
            mealType: normalizeMealType(response.mealType),
            comment: response.comment,
            imageUrl: response.imageUrl ?? null,
            imageAssetId: response.imageAssetId ?? null,
            totalCalories: response.totalCalories,
            totalProteins: response.totalProteins,
            totalFats: response.totalFats,
            totalCarbs: response.totalCarbs,
            totalFiber: response.totalFiber,
            totalAlcohol: response.totalAlcohol,
            isNutritionAutoCalculated,
            manualCalories: response.manualCalories ?? null,
            manualProteins: response.manualProteins ?? null,
            manualFats: response.manualFats ?? null,
            manualCarbs: response.manualCarbs ?? null,
            manualFiber: response.manualFiber ?? null,
            manualAlcohol: response.manualAlcohol ?? null,
            preMealSatietyLevel: this.normalizeSatietyLevel(response.preMealSatietyLevel),
            postMealSatietyLevel: this.normalizeSatietyLevel(response.postMealSatietyLevel),
            qualityScore: response.qualityScore ?? null,
            qualityGrade: response.qualityGrade ?? null,
            isFavorite: response.isFavorite ?? false,
            favoriteMealId: response.favoriteMealId ?? null,
            items: response.items.map(item => this.mapConsumptionItem(item)),
            aiSessions: response.aiSessions?.map(session => this.mapAiSession(session)) ?? [],
        };
    }

    private resolveIsNutritionAutoCalculated(response: ConsumptionResponseDto): boolean {
        const isAuto = response.isNutritionAutoCalculated;
        if (isAuto || response.items.length > 0 || !this.hasAiItems(response)) {
            return isAuto;
        }

        const aiTotals = this.calculateAiTotals(response);
        return (
            this.areClose(response.manualCalories ?? response.totalCalories, aiTotals.calories) &&
            this.areClose(response.manualProteins ?? response.totalProteins, aiTotals.proteins) &&
            this.areClose(response.manualFats ?? response.totalFats, aiTotals.fats) &&
            this.areClose(response.manualCarbs ?? response.totalCarbs, aiTotals.carbs) &&
            this.areClose(response.manualFiber ?? response.totalFiber, aiTotals.fiber) &&
            this.areClose(response.manualAlcohol ?? response.totalAlcohol, aiTotals.alcohol)
        );
    }

    private normalizeSatietyLevel(value: number | null | undefined): number {
        if (value === null || value === undefined || value <= 0 || Number.isNaN(value)) {
            return DEFAULT_SATIETY_LEVEL;
        }

        if (value > MAX_SATIETY_LEVEL) {
            return Math.min(MAX_SATIETY_LEVEL, Math.max(MIN_SATIETY_LEVEL, Math.round(value / SATIETY_NORMALIZATION_DIVISOR)));
        }

        return Math.max(MIN_SATIETY_LEVEL, value);
    }

    private hasAiItems(response: ConsumptionResponseDto): boolean {
        return response.aiSessions?.some(session => session.items.length > 0) ?? false;
    }

    private calculateAiTotals(response: ConsumptionResponseDto): {
        calories: number;
        proteins: number;
        fats: number;
        carbs: number;
        fiber: number;
        alcohol: number;
    } {
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
        return Math.abs(left - right) <= NUTRITION_CLOSE_TOLERANCE;
    }

    private mapConsumptionItem(response: ConsumptionItemResponseDto): ConsumptionItem {
        const product =
            response.productId !== null && response.productId !== undefined && response.productId.length > 0
                ? this.createProductFromSnapshot(response)
                : null;
        const recipe =
            response.recipeId !== null && response.recipeId !== undefined && response.recipeId.length > 0
                ? this.createRecipeFromSnapshot(response)
                : null;
        const sourceType = product !== null ? ConsumptionSourceType.Product : ConsumptionSourceType.Recipe;

        return {
            id: response.id,
            consumptionId: response.consumptionId,
            amount: response.amount,
            sourceType,
            product,
            recipe,
        };
    }

    private createProductFromSnapshot(response: ConsumptionItemResponseDto): Product {
        const base = createEmptyProductSnapshot();
        return {
            ...base,
            id: response.productId ?? '',
            name: response.productName ?? '',
            imageUrl: response.productImageUrl ?? null,
            baseUnit: this.normalizeMeasurementUnit(response.productBaseUnit),
            baseAmount: response.productBaseAmount ?? DEFAULT_ITEM_AMOUNT,
            defaultPortionAmount: response.productBaseAmount ?? DEFAULT_ITEM_AMOUNT,
            caloriesPerBase: response.productCaloriesPerBase ?? EMPTY_NUTRITION_VALUE,
            proteinsPerBase: response.productProteinsPerBase ?? EMPTY_NUTRITION_VALUE,
            fatsPerBase: response.productFatsPerBase ?? EMPTY_NUTRITION_VALUE,
            carbsPerBase: response.productCarbsPerBase ?? EMPTY_NUTRITION_VALUE,
            fiberPerBase: response.productFiberPerBase ?? EMPTY_NUTRITION_VALUE,
            alcoholPerBase: response.productAlcoholPerBase ?? EMPTY_NUTRITION_VALUE,
        };
    }

    private createRecipeFromSnapshot(response: ConsumptionItemResponseDto): Recipe {
        const base = createEmptyRecipeSnapshot();
        return {
            ...base,
            id: response.recipeId ?? '',
            name: response.recipeName ?? '',
            imageUrl: response.recipeImageUrl ?? null,
            servings: response.recipeServings ?? DEFAULT_ITEM_AMOUNT,
            totalCalories: response.recipeTotalCalories ?? EMPTY_NUTRITION_VALUE,
            totalProteins: response.recipeTotalProteins ?? EMPTY_NUTRITION_VALUE,
            totalFats: response.recipeTotalFats ?? EMPTY_NUTRITION_VALUE,
            totalCarbs: response.recipeTotalCarbs ?? EMPTY_NUTRITION_VALUE,
            totalFiber: response.recipeTotalFiber ?? EMPTY_NUTRITION_VALUE,
            totalAlcohol: response.recipeTotalAlcohol ?? EMPTY_NUTRITION_VALUE,
        };
    }

    private mapAiSession(response: ConsumptionAiSessionResponseDto): ConsumptionAiSession {
        return {
            id: response.id,
            consumptionId: response.consumptionId,
            imageAssetId: response.imageAssetId ?? null,
            imageUrl: response.imageUrl ?? null,
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
            })),
        };
    }

    private normalizeMeasurementUnit(unit?: MeasurementUnit | string | null): MeasurementUnit {
        if (unit === null || unit === undefined || unit.length === 0) {
            return MeasurementUnit.G;
        }

        const normalized = unit.toString().toUpperCase();
        if (normalized in MeasurementUnit) {
            return normalized as MeasurementUnit;
        }

        return MeasurementUnit.G;
    }

    private createEmptyNutritionTotals(): {
        calories: number;
        proteins: number;
        fats: number;
        carbs: number;
        fiber: number;
        alcohol: number;
    } {
        return {
            calories: EMPTY_NUTRITION_VALUE,
            proteins: EMPTY_NUTRITION_VALUE,
            fats: EMPTY_NUTRITION_VALUE,
            carbs: EMPTY_NUTRITION_VALUE,
            fiber: EMPTY_NUTRITION_VALUE,
            alcohol: EMPTY_NUTRITION_VALUE,
        };
    }
}
