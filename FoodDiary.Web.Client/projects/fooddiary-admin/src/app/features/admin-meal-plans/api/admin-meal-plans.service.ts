import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { loadPagedCollection } from '../../../shared/api/load-paged-collection';
import type { CatalogPlan, CatalogPlanRequest, CatalogRecipe } from '../models/admin-meal-plan.data';

@Service()
export class AdminMealPlansService {
    private readonly http = inject(HttpClient);
    private readonly url = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/meal-plans`;

    public getAll(): Observable<CatalogPlan[]> {
        return loadPagedCollection((page, limit) => this.http.get<CatalogPlan[]>(this.url, { params: { page, limit } }));
    }

    public get(id: string): Observable<CatalogPlan> {
        return this.http.get<CatalogPlan>(`${this.url}/${id}`);
    }

    public save(id: string | null, request: CatalogPlanRequest): Observable<CatalogPlan> {
        return id === null ? this.http.post<CatalogPlan>(this.url, request) : this.http.put<CatalogPlan>(`${this.url}/${id}`, request);
    }

    public recipes(search: string): Observable<CatalogRecipe[]> {
        return this.http.get<CatalogRecipe[]>(`${this.url}/recipes`, { params: { search } });
    }
}
