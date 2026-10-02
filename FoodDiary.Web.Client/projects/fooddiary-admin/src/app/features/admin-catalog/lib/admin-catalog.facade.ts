import { inject, Injectable } from '@angular/core';
import type { Observable } from 'rxjs';

import { AdminCatalogService } from '../api/admin-catalog.service';
import type { CatalogKind, CatalogProduct, CatalogRecipe, CatalogResult } from '../models/catalog-transfer';

@Injectable()
export class AdminCatalogFacade {
    private readonly api = inject(AdminCatalogService);

    public exportCatalog(): Observable<{ products: CatalogProduct[]; recipes: CatalogRecipe[] }> {
        return this.api.exportCatalog();
    }

    public preview(kind: CatalogKind, item: CatalogProduct | CatalogRecipe): Observable<CatalogResult> {
        return this.api.preview(kind, item);
    }

    public importItem(kind: CatalogKind, item: CatalogProduct | CatalogRecipe): Observable<CatalogResult> {
        return this.api.importItem(kind, item);
    }
}
