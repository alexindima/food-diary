import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { catchError, Observable, throwError } from 'rxjs';
import { ApiService } from './api.service';
import { HttpErrorResponse } from '@angular/common/http';
import { ShoppingList, ShoppingListCreateDto, ShoppingListSummary, ShoppingListUpdateDto } from '../types/shopping-list.data';

@Injectable({
    providedIn: 'root',
})
export class ShoppingListService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.shoppingLists;

    public getCurrent(): Observable<ShoppingList> {
        return this.get<ShoppingList>('current').pipe(
            catchError((error: HttpErrorResponse) => {
                console.error('Get current shopping list error', error);
                return throwError(() => error);
            }),
        );
    }

    public getAll(): Observable<ShoppingListSummary[]> {
        return this.get<ShoppingListSummary[]>('').pipe(
            catchError((error: HttpErrorResponse) => {
                console.error('Get shopping lists error', error);
                return throwError(() => error);
            }),
        );
    }

    public getById(id: string): Observable<ShoppingList> {
        return this.get<ShoppingList>(`${id}`).pipe(
            catchError((error: HttpErrorResponse) => {
                console.error('Get shopping list error', error);
                return throwError(() => error);
            }),
        );
    }

    public create(data: ShoppingListCreateDto): Observable<ShoppingList> {
        return this.post<ShoppingList>('', data).pipe(
            catchError((error: HttpErrorResponse) => {
                console.error('Create shopping list error', error);
                return throwError(() => error);
            }),
        );
    }

    public update(id: string, data: ShoppingListUpdateDto): Observable<ShoppingList> {
        return this.patch<ShoppingList>(`${id}`, data).pipe(
            catchError((error: HttpErrorResponse) => {
                console.error('Update shopping list error', error);
                return throwError(() => error);
            }),
        );
    }

    public deleteById(id: string): Observable<void> {
        return this.delete<void>(`${id}`).pipe(
            catchError((error: HttpErrorResponse) => {
                console.error('Delete shopping list error', error);
                return throwError(() => error);
            }),
        );
    }
}
