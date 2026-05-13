import { Injectable } from '@angular/core';
import { catchError, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import type { ShoppingList, ShoppingListCreateDto, ShoppingListSummary, ShoppingListUpdateDto } from '../models/shopping-list.data';

@Injectable({ providedIn: 'root' })
export class ShoppingListService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.shoppingLists;

    public getCurrent(): Observable<ShoppingList | null> {
        return this.get<ShoppingList>('current').pipe(
            catchError((error: unknown) => fallbackApiError('Get current shopping list error', error, null)),
        );
    }

    public getAll(): Observable<ShoppingListSummary[]> {
        return this.get<ShoppingListSummary[]>('').pipe(
            catchError((error: unknown) => fallbackApiError('Get shopping lists error', error, [])),
        );
    }

    public getById(id: string): Observable<ShoppingList | null> {
        return this.get<ShoppingList>(id).pipe(catchError((error: unknown) => fallbackApiError('Get shopping list error', error, null)));
    }

    public create(data: ShoppingListCreateDto): Observable<ShoppingList> {
        return this.post<ShoppingList>('', data).pipe(catchError((error: unknown) => rethrowApiError('Create shopping list error', error)));
    }

    public update(id: string, data: ShoppingListUpdateDto): Observable<ShoppingList> {
        return this.patch<ShoppingList>(id, data).pipe(
            catchError((error: unknown) => rethrowApiError('Update shopping list error', error)),
        );
    }

    public deleteById(id: string): Observable<void> {
        return this.delete<void>(id).pipe(catchError((error: unknown) => rethrowApiError('Delete shopping list error', error)));
    }
}
