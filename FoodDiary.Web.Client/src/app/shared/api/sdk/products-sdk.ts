import type { HttpClient } from '@angular/common/http';

import { ProductsSdk } from './generated/api/products.service';
import { Configuration } from './generated/configuration';

export function createProductsSdk(baseUrl: string, http: HttpClient): { client: ProductsSdk; version: string } {
    const route = /^(.*)\/api\/v([^/]+)\/products\/?$/u.exec(baseUrl);
    if (route === null) {
        throw new Error('Products API URL must end with /api/v{version}/products.');
    }
    const [, basePath, version] = route;
    const client = new ProductsSdk(http, basePath, new Configuration({ withCredentials: true }));
    return { client, version };
}
