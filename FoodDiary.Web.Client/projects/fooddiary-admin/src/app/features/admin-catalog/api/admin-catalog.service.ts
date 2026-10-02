import { HttpClient, HttpHeaders } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { forkJoin, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import type { CatalogKind, CatalogProduct, CatalogRecipe, CatalogResult } from '../models/catalog-transfer';

@Service()
export class AdminCatalogService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/catalog`;

    public exportCatalog(): Observable<{ products: CatalogProduct[]; recipes: CatalogRecipe[] }> {
        return forkJoin({
            products: this.http.get<CatalogProduct[]>(`${this.baseUrl}/products/export`),
            recipes: this.http.get<CatalogRecipe[]>(`${this.baseUrl}/recipes/export`),
        });
    }

    public preview(kind: CatalogKind, item: CatalogProduct | CatalogRecipe): Observable<CatalogResult> {
        return this.http.post<CatalogResult>(`${this.baseUrl}/${kind}/preview`, item);
    }

    public importItem(kind: CatalogKind, item: CatalogProduct | CatalogRecipe): Observable<CatalogResult> {
        const headers = new HttpHeaders({ 'Idempotency-Key': crypto.randomUUID() });
        return this.http.post<CatalogResult>(`${this.baseUrl}/${kind}/import`, item, { headers });
    }
}
