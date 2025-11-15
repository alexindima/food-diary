import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { environment } from '../../environments/environment';
import {
    Consumption,
    ConsumptionFilters,
    ConsumptionItem,
    ConsumptionItemResponseDto,
    ConsumptionManageDto,
    ConsumptionResponseDto,
    ConsumptionSourceType,
    createEmptyProductSnapshot,
    createEmptyRecipeSnapshot
} from '../types/consumption.data';
import { catchError, map, Observable, of } from 'rxjs';
import { PageOf } from '../types/page-of.data';
import { Product, MeasurementUnit } from '../types/product.data';
import { Recipe } from '../types/recipe.data';

@Injectable({
    providedIn: 'root',
})
export class ConsumptionService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.consumptions;

    public query(page: number, limit: number, filters: ConsumptionFilters): Observable<PageOf<Consumption>> {
        const params = { page, limit, ...filters };
        return this.get<PageOf<ConsumptionResponseDto>>('', params).pipe(
            map(pageData => ({
                ...pageData,
                data: pageData.data.map(response => this.mapConsumption(response)),
            })),
            catchError(() => {
                const empty: PageOf<Consumption> = {
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

    public getById(id: number): Observable<Consumption | null> {
        return this.get<ConsumptionResponseDto>(`${id}`).pipe(
            map(response => this.mapConsumption(response)),
            catchError(() => of(null)),
        );
    }

    public create(data: ConsumptionManageDto): Observable<Consumption | null> {
        return this.post<ConsumptionResponseDto>('', data).pipe(
            map(response => this.mapConsumption(response)),
            catchError(() => of(null)),
        );
    }

    public update(id: number, data: ConsumptionManageDto): Observable<Consumption | null> {
        return this.patch<ConsumptionResponseDto>(`${id}`, data).pipe(
            map(response => this.mapConsumption(response)),
            catchError(() => of(null)),
        );
    }

    public deleteById(id: number): Observable<void> {
        return this.delete<void>(`${id}`).pipe(catchError(() => of(void 0)));
    }

    private mapConsumption(response: ConsumptionResponseDto): Consumption {
        return {
            id: response.id,
            date: response.date,
            mealType: response.mealType,
            comment: response.comment,
            imageUrl: response.imageUrl ?? '',
            totalCalories: response.totalCalories,
            totalProteins: response.totalProteins,
            totalFats: response.totalFats,
            totalCarbs: response.totalCarbs,
            totalFiber: response.totalFiber,
            isNutritionAutoCalculated: response.isNutritionAutoCalculated ?? true,
            manualCalories: response.manualCalories ?? null,
            manualProteins: response.manualProteins ?? null,
            manualFats: response.manualFats ?? null,
            manualCarbs: response.manualCarbs ?? null,
            manualFiber: response.manualFiber ?? null,
            items: response.items.map(item => this.mapConsumptionItem(item)),
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
            caloriesPerBase: response.productCaloriesPerBase ?? 0,
            proteinsPerBase: response.productProteinsPerBase ?? 0,
            fatsPerBase: response.productFatsPerBase ?? 0,
            carbsPerBase: response.productCarbsPerBase ?? 0,
            fiberPerBase: response.productFiberPerBase ?? 0,
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
