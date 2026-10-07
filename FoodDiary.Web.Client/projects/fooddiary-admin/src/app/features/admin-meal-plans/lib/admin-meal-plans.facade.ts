import { inject, Injectable } from '@angular/core';
import type { Observable } from 'rxjs';

import { AdminMealPlansService } from '../api/admin-meal-plans.service';
import type { CatalogPlan, CatalogPlanRequest, CatalogPlanSummary, CatalogRecipe } from '../models/admin-meal-plan.data';

@Injectable()
export class AdminMealPlansFacade {
    private readonly api = inject(AdminMealPlansService);
    public getAll(): Observable<CatalogPlanSummary[]> {
        return this.api.getAll();
    }
    public get(id: string): Observable<CatalogPlan> {
        return this.api.get(id);
    }
    public save(id: string | null, request: CatalogPlanRequest): Observable<CatalogPlan> {
        return this.api.save(id, request);
    }
    public recipes(search: string): Observable<CatalogRecipe[]> {
        return this.api.recipes(search);
    }
}
