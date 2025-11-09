import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { environment } from '../../environments/environment';
import { catchError, Observable, throwError } from 'rxjs';
import { Product, CreateProductRequest, ProductFilters } from '../types/product.data';
import { HttpErrorResponse } from '@angular/common/http';
import { PageOf } from '../types/page-of.data';

@Injectable({
    providedIn: 'root',
})
export class ProductService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.products;

    public query(page: number, limit: number, filters?: ProductFilters): Observable<PageOf<Product>> {
        const params: Record<string, string | number> = { page, limit };
        const search = filters?.search?.trim();
        if (search) {
            params['search'] = search;
        }

        return this.get<PageOf<Product>>('', params).pipe(
            catchError((error: HttpErrorResponse) => {
                console.error('Query products error', error);
                return throwError(() => error);
            }),
        );
    }

    public getById(id: string): Observable<Product> {
        return this.get<Product>(`${id}`).pipe(
            catchError((error: HttpErrorResponse) => {
                console.error('Get product error', error);
                return throwError(() => error);
            }),
        );
    }

    public create(data: CreateProductRequest): Observable<Product> {
        return this.post<Product>('', data).pipe(
            catchError((error: HttpErrorResponse) => {
                console.error('Create product error', error);
                return throwError(() => error);
            }),
        );
    }

    public update(id: string, data: Partial<CreateProductRequest>): Observable<Product> {
        return this.patch<Product>(`${id}`, data).pipe(
            catchError((error: HttpErrorResponse) => {
                console.error('Update product error', error);
                return throwError(() => error);
            }),
        );
    }

    public deleteById(id: string): Observable<void> {
        return this.delete<void>(`${id}`).pipe(
            catchError((error: HttpErrorResponse) => {
                console.error('Delete product error', error);
                return throwError(() => error);
            }),
        );
    }
}
