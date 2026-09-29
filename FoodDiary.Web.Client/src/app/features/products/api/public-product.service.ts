import { Service } from '@angular/core';
import type { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import type { PublicProduct } from '../models/public-product.data';

@Service()
export class PublicProductService extends ApiService {
    protected readonly baseUrl = `${environment.apiUrls.products}/public`;
    public getById(id: string): Observable<PublicProduct> {
        return this.get<PublicProduct>(encodeURIComponent(id));
    }
}
