import { formatDate } from '@angular/common';
import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { computed, DestroyRef, inject, Injectable, LOCALE_ID, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { catchError, type Observable, of, Subject, switchMap, takeUntil, throwError } from 'rxjs';

import {
    ConfirmDeleteDialogComponent,
    type ConfirmDeleteDialogData,
} from '../../../../components/shared/confirm-delete-dialog/confirm-delete-dialog';
import type { Meal } from '../../../../shared/models/meal.data';
import { entityId, type MealId } from '../../../../shared/models/semantics/entity-id';
import { FavoriteMealService } from '../../api/favorite-meal.service';
import { MealDetailActionResult } from '../../components/detail/meal-detail-lib/meal-detail.types';

@Injectable()
export class MealDetailFacade {
    private readonly destroyRef = inject(DestroyRef);
    private readonly favoriteStatusRequests = new Subject<void>();
    private readonly dialogRef = inject(FdUiDialogRef<unknown, MealDetailActionResult>);
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly locale = inject(LOCALE_ID);
    private readonly translate = inject(TranslateService);
    private readonly favoriteMealService = inject(FavoriteMealService);

    public readonly isFavorite = signal(false);
    public readonly isFavoriteLoading = signal(false);
    public readonly favoriteIcon = computed(() => (this.isFavorite() ? 'star' : 'star_border'));
    public readonly favoriteAriaLabelKey = computed(() => (this.isFavorite() ? 'MEAL_DETAIL.REMOVE_FAVORITE' : 'MEAL_DETAIL.ADD_FAVORITE'));

    private initialFavoriteState = false;
    private favoriteMealId: string | null = null;

    public initialize(meal: Meal): void {
        this.favoriteStatusRequests.next();
        this.initialFavoriteState = meal.isFavorite ?? false;
        this.isFavorite.set(this.initialFavoriteState);
        this.favoriteMealId = meal.favoriteMealId ?? null;

        this.favoriteMealService
            .isFavorite(meal.id)
            .pipe(
                catchError(() => of(this.initialFavoriteState)),
                takeUntil(this.favoriteStatusRequests),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(isFavorite => {
                this.initialFavoriteState = isFavorite;
                this.isFavorite.set(isFavorite);
            });
    }

    public close(meal: Meal): void {
        if (this.hasFavoriteChanged()) {
            this.dialogRef.close(new MealDetailActionResult(meal.id, 'FavoriteChanged', true));
            return;
        }

        this.dialogRef.close();
    }

    public edit(meal: Meal): void {
        this.dialogRef.close(new MealDetailActionResult(meal.id, 'Edit', this.hasFavoriteChanged()));
    }

    public repeat(meal: Meal): void {
        this.dialogRef.close(new MealDetailActionResult(meal.id, 'Repeat', this.hasFavoriteChanged()));
    }

    public delete(meal: Meal): void {
        const formattedDate = formatDate(meal.date, 'dd.MM.yyyy', this.locale);
        const data: ConfirmDeleteDialogData = {
            title: this.translate.instant('CONFIRM_DELETE.TITLE', {
                type: this.translate.instant('MEAL_DETAIL.ENTITY_NAME'),
            }),
            message: this.translate.instant('CONFIRM_DELETE.MESSAGE', { name: formattedDate }),
            name: formattedDate,
            entityType: this.translate.instant('MEAL_DETAIL.ENTITY_NAME'),
            confirmLabel: this.translate.instant('CONFIRM_DELETE.CONFIRM'),
            cancelLabel: this.translate.instant('CONFIRM_DELETE.CANCEL'),
        };

        const confirmation = this.fdDialogService.open(ConfirmDeleteDialogComponent, { data, size: 'sm' });
        const unregisterClose = this.destroyRef.onDestroy(() => {
            confirmation.close();
        });
        confirmation
            .afterClosed()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(confirm => {
                if (this.destroyRef.destroyed) {
                    return;
                }
                unregisterClose();
                if (confirm === true) {
                    this.dialogRef.close(new MealDetailActionResult(meal.id, 'Delete', this.hasFavoriteChanged()));
                }
            });
    }

    public toggleFavorite(meal: Meal): void {
        if (this.isFavoriteLoading()) {
            return;
        }

        this.favoriteStatusRequests.next();
        this.isFavoriteLoading.set(true);

        if (this.isFavorite()) {
            this.removeFavorite(meal);
            return;
        }

        this.favoriteMealService
            .add(meal.id)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: favorite => {
                    this.isFavorite.set(true);
                    this.favoriteMealId = favorite.id;
                    this.isFavoriteLoading.set(false);
                },
                error: () => {
                    this.isFavoriteLoading.set(false);
                },
            });
    }

    private removeFavorite(meal: Meal): void {
        const favoriteId = this.favoriteMealId;
        const removal: Observable<void | null> =
            favoriteId !== null && favoriteId.length > 0
                ? this.favoriteMealService.remove(entityId<'favorite-meal'>(favoriteId)).pipe(
                      catchError((error: unknown) => {
                          if (!(error instanceof HttpErrorResponse) || error.status !== Number(HttpStatusCode.NotFound)) {
                              return throwError(() => error);
                          }

                          this.favoriteMealId = null;
                          return this.removeFavoriteByMealId(meal.id);
                      }),
                  )
                : this.removeFavoriteByMealId(meal.id);

        removal.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
            next: () => {
                this.isFavorite.set(false);
                this.favoriteMealId = null;
                this.isFavoriteLoading.set(false);
            },
            error: () => {
                this.isFavoriteLoading.set(false);
            },
        });
    }

    private removeFavoriteByMealId(mealId: MealId): Observable<void | null> {
        return this.favoriteMealService.getLookupPage().pipe(
            switchMap(favorites => {
                const match = favorites.find(favorite => favorite.mealId === mealId);
                return match === undefined ? of(null) : this.favoriteMealService.remove(match.id);
            }),
        );
    }

    private hasFavoriteChanged(): boolean {
        return this.initialFavoriteState !== this.isFavorite();
    }
}
