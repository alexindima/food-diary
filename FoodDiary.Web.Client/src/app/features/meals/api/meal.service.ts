import { Injectable } from '@angular/core';
import { catchError, map, Observable, of } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { normalizeMealType } from '../../../shared/lib/meal-type.util';
import { PageOf } from '../../../shared/models/page-of.data';
import { MeasurementUnit, Product } from '../../products/models/product.data';
import { Recipe } from '../../recipes/models/recipe.data';
import {
    Consumption,
    ConsumptionAiSession,
    ConsumptionAiSessionResponseDto,
    ConsumptionItem,
    ConsumptionItemResponseDto,
    ConsumptionOverview,
    ConsumptionResponseDto,
    ConsumptionSourceType,
    createEmptyProductSnapshot,
    createEmptyRecipeSnapshot,
    Meal,
    MealFilters,
    MealManageDto,
    MealOverview,
} from '../models/meal.data';

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
            catchError(() => {
                const empty: PageOf<Meal> = {
                    data: [],
                    page,
                    limit,
                    totalPages: 0,
                    totalItems: 0,
                };
                return of(empty);
            }),
        );
    }

    public queryOverview(page: number, limit: number, filters: MealFilters, favoriteLimit = 10): Observable<MealOverview> {
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
            catchError(() => {
                const empty: MealOverview = {
                    allConsumptions: {
                        data: [],
                        page,
                        limit,
                        totalPages: 0,
                        totalItems: 0,
                    },
                    favoriteItems: [],
                    favoriteTotalCount: 0,
                };
                return of(empty);
            }),
        );
    }

    public getById(id: string): Observable<Meal | null> {
        return this.get<ConsumptionResponseDto>(`${id}`).pipe(
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
        return this.patch<ConsumptionResponseDto>(`${id}`, data).pipe(
            map(response => this.mapConsumption(response)),
            catchError(() => of(null)),
        );
    }

    public deleteById(id: string): Observable<void> {
        return this.delete<void>(`${id}`).pipe(catchError(() => of(void 0)));
    }

    public repeat(id: string, targetDate: string, mealType?: string): Observable<Meal | null> {
        return this.post<ConsumptionResponseDto>(`${id}/repeat`, { targetDate, mealType }).pipe(
            map(response => this.mapConsumption(response)),
            catchError(() => of(null)),
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
            totalAlcohol: response.totalAlcohol ?? 0,
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
        const isAuto = response.isNutritionAutoCalculated ?? true;
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
            this.areClose(response.manualAlcohol ?? response.totalAlcohol ?? 0, aiTotals.alcohol)
        );
    }

    private normalizeSatietyLevel(value: number | null | undefined): number {
        if (!value) {
            return 3;
        }

        if (value > 5) {
            return Math.min(5, Math.max(1, Math.round(value / 2)));
        }

        return Math.max(1, value);
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
                { calories: 0, proteins: 0, fats: 0, carbs: 0, fiber: 0, alcohol: 0 },
            ) ?? { calories: 0, proteins: 0, fats: 0, carbs: 0, fiber: 0, alcohol: 0 }
        );
    }

    private areClose(left: number, right: number): boolean {
        return Math.abs(left - right) <= 0.000001;
    }

    private mapConsumptionItem(response: ConsumptionItemResponseDto): ConsumptionItem {
        const product = response.productId ? this.createProductFromSnapshot(response) : null;
        const recipe = response.recipeId ? this.createRecipeFromSnapshot(response) : null;
        const sourceType = product ? ConsumptionSourceType.Product : ConsumptionSourceType.Recipe;

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
            baseAmount: response.productBaseAmount ?? 1,
            defaultPortionAmount: response.productBaseAmount ?? 1,
            caloriesPerBase: response.productCaloriesPerBase ?? 0,
            proteinsPerBase: response.productProteinsPerBase ?? 0,
            fatsPerBase: response.productFatsPerBase ?? 0,
            carbsPerBase: response.productCarbsPerBase ?? 0,
            fiberPerBase: response.productFiberPerBase ?? 0,
            alcoholPerBase: response.productAlcoholPerBase ?? 0,
        };
    }

    private createRecipeFromSnapshot(response: ConsumptionItemResponseDto): Recipe {
        const base = createEmptyRecipeSnapshot();
        return {
            ...base,
            id: response.recipeId ?? '',
            name: response.recipeName ?? '',
            imageUrl: response.recipeImageUrl ?? null,
            servings: response.recipeServings ?? 1,
            totalCalories: response.recipeTotalCalories ?? 0,
            totalProteins: response.recipeTotalProteins ?? 0,
            totalFats: response.recipeTotalFats ?? 0,
            totalCarbs: response.recipeTotalCarbs ?? 0,
            totalFiber: response.recipeTotalFiber ?? 0,
            totalAlcohol: response.recipeTotalAlcohol ?? 0,
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
        if (!unit) {
            return MeasurementUnit.G;
        }

        const normalized = unit.toString().toUpperCase();
        if (normalized in MeasurementUnit) {
            return normalized as MeasurementUnit;
        }

        return MeasurementUnit.G;
    }
}
