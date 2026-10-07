import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { catchError, EMPTY, expand, map, type Observable, reduce } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ShoppingListsSdk } from '../../../shared/api/sdk/generated/api/shopping-lists.service';
import { createSdkConnection } from '../../../shared/api/sdk/sdk-connection';
import { sdkOptional } from '../../../shared/api/sdk/sdk-response';
import { shoppingListFromSdk, shoppingOverviewFromSdk, shoppingSummaryFromSdk } from '../../../shared/api/sdk/shopping-sdk.mapper';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import type {
    ShoppingList,
    ShoppingListCreateDto,
    ShoppingListOverview,
    ShoppingListSummary,
    ShoppingListUpdateDto,
} from '../../../shared/models/shopping-list.data';

const DEFAULT_PAGE_SIZE = 20;

const SELECTION_PAGE_SIZE = 50;

@Service()
export class ShoppingListService {
    protected readonly baseUrl = environment.apiUrls.shoppingLists;
    private readonly sdk = createSdkConnection(ShoppingListsSdk, this.baseUrl, inject(HttpClient));

    public getOverview(): Observable<ShoppingListOverview> {
        return this.sdk.client.getShoppingListsOverview({ version: this.sdk.version }).pipe(map(shoppingOverviewFromSdk));
    }

    public getCurrent(): Observable<ShoppingList | null> {
        return this.sdk.client.getShoppingListsCurrent({ version: this.sdk.version }).pipe(
            map(value => sdkOptional(value, shoppingListFromSdk)),
            catchError((error: unknown) => fallbackApiError('Get current shopping list error', error, null)),
        );
    }

    public getPage(page = 1, search = ''): Observable<ShoppingListSummary[]> {
        return this.requestPage(page, DEFAULT_PAGE_SIZE, search).pipe(
            catchError((error: unknown) => rethrowApiError('Get shopping lists error', error)),
        );
    }

    public getSelectionPage(): Observable<ShoppingListSummary[]> {
        return this.requestPage(1, SELECTION_PAGE_SIZE).pipe(
            expand((items, index) => (items.length === SELECTION_PAGE_SIZE ? this.requestPage(index + 2, SELECTION_PAGE_SIZE) : EMPTY)),
            reduce((allItems, items) => [...allItems, ...items], [] as ShoppingListSummary[]),
        );
    }

    public getById(id: string): Observable<ShoppingList | null> {
        return this.sdk.client.getShoppingListsById({ version: this.sdk.version, id }).pipe(
            map(value => sdkOptional(value, shoppingListFromSdk)),
            catchError((error: unknown) => fallbackApiError('Get shopping list error', error, null)),
        );
    }

    public create(data: ShoppingListCreateDto): Observable<ShoppingList> {
        return this.sdk.client.postShoppingLists({ version: this.sdk.version, createShoppingListHttpRequest: data }).pipe(
            map(shoppingListFromSdk),
            catchError((error: unknown) => rethrowApiError('Create shopping list error', error)),
        );
    }

    public update(id: string, data: ShoppingListUpdateDto): Observable<ShoppingList> {
        return this.sdk.client.patchShoppingListsById({ version: this.sdk.version, id, updateShoppingListHttpRequest: data }).pipe(
            map(shoppingListFromSdk),
            catchError((error: unknown) => rethrowApiError('Update shopping list error', error)),
        );
    }

    public deleteById(id: string): Observable<void> {
        return this.sdk.client.deleteShoppingListsById({ version: this.sdk.version, id }).pipe(
            map(() => {}),
            catchError((error: unknown) => rethrowApiError('Delete shopping list error', error)),
        );
    }

    private requestPage(page: number, limit: number, search?: string): Observable<ShoppingListSummary[]> {
        return this.sdk.client
            .getShoppingListsPage({ version: this.sdk.version, page, limit, search })
            .pipe(map(values => values.map(shoppingSummaryFromSdk)));
    }
}
