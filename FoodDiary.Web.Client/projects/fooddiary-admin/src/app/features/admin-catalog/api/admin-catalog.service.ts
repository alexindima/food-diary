import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { forkJoin, map, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { AdminCatalogSdk } from '../../../shared/api/sdk/generated/api/admin-catalog.service';
import { createSdkConnection } from '../../../shared/api/sdk/sdk-connection';
import type { CatalogKind, CatalogProduct, CatalogRecipe, CatalogResult } from '../models/catalog-transfer';
import {
    catalogProductFromSdk,
    catalogProductImportFromSdk,
    catalogRecipeExportFromSdk,
    catalogRecipeImportFromSdk,
} from './admin-catalog-sdk.mapper';

@Service()
export class AdminCatalogService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/catalog`;
    private readonly sdk = createSdkConnection(AdminCatalogSdk, this.baseUrl, this.http);

    public exportCatalog(): Observable<{ products: CatalogProduct[]; recipes: CatalogRecipe[] }> {
        return forkJoin({
            products: this.sdk.client
                .getAdminCatalogProductsExport({ version: this.sdk.version })
                .pipe(map(items => items.map(catalogProductFromSdk))),
            recipes: this.sdk.client
                .getAdminCatalogRecipesExport({ version: this.sdk.version })
                .pipe(map(items => items.map(catalogRecipeExportFromSdk))),
        });
    }

    public preview(kind: CatalogKind, item: CatalogProduct | CatalogRecipe): Observable<CatalogResult> {
        return kind === 'products'
            ? this.sdk.client
                  .postAdminCatalogProductsPreview({ version: this.sdk.version, catalogProductHttpRequest: item })
                  .pipe(map(catalogProductImportFromSdk))
            : this.sdk.client
                  .postAdminCatalogRecipesPreview({ version: this.sdk.version, catalogRecipeHttpRequest: item })
                  .pipe(map(catalogRecipeImportFromSdk));
    }

    public importItem(kind: CatalogKind, item: CatalogProduct | CatalogRecipe): Observable<CatalogResult> {
        const idempotencyKey = crypto.randomUUID();
        return kind === 'products'
            ? this.sdk.client
                  .postAdminCatalogProductsImport({ version: this.sdk.version, catalogProductHttpRequest: item, idempotencyKey })
                  .pipe(map(catalogProductImportFromSdk))
            : this.sdk.client
                  .postAdminCatalogRecipesImport({ version: this.sdk.version, catalogRecipeHttpRequest: item, idempotencyKey })
                  .pipe(map(catalogRecipeImportFromSdk));
    }
}
