import { Injectable } from '@angular/core';
import { catchError, map, Observable, of } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
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
        return {
            id: response.id,
            date: response.date,
            mealType: response.mealType,
            comment: response.comment,
            imageUrl: response.imageUrl ?? null,
            imageAssetId: response.imageAssetId ?? null,
            totalCalories: response.totalCalories,
            totalProteins: response.totalProteins,
            totalFats: response.totalFats,
            totalCarbs: response.totalCarbs,
            totalFiber: response.totalFiber,
            totalAlcohol: response.totalAlcohol ?? 0,
            isNutritionAutoCalculated: response.isNutritionAutoCalculated ?? true,
            manualCalories: response.manualCalories ?? null,
            manualProteins: response.manualProteins ?? null,
            manualFats: response.manualFats ?? null,
            manualCarbs: response.manualCarbs ?? null,
            manualFiber: response.manualFiber ?? null,
            manualAlcohol: response.manualAlcohol ?? null,
            preMealSatietyLevel: response.preMealSatietyLevel ?? 0,
            postMealSatietyLevel: response.postMealSatietyLevel ?? 0,
            qualityScore: response.qualityScore ?? null,
            qualityGrade: response.qualityGrade ?? null,
            isFavorite: response.isFavorite ?? false,
            favoriteMealId: response.favoriteMealId ?? null,
            items: response.items.map(item => this.mapConsumptionItem(item)),
            aiSessions: response.aiSessions?.map(session => this.mapAiSession(session)) ?? [],
        };
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
