import { DatePipe } from '@angular/common';
import { computed, inject, Injectable, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { of, switchMap } from 'rxjs';

import {
    ConfirmDeleteDialogComponent,
    type ConfirmDeleteDialogData,
} from '../../../../../components/shared/confirm-delete-dialog/confirm-delete-dialog.component';
import { FavoriteMealService } from '../../../api/favorite-meal.service';
import type { Meal } from '../../../models/meal.data';
import { MealDetailActionResult } from './meal-detail.types';

@Injectable({ providedIn: 'root' })
export class MealDetailFacade {
    private readonly dialogRef = inject(FdUiDialogRef<unknown, MealDetailActionResult>);
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly datePipe = inject(DatePipe);
    private readonly translate = inject(TranslateService);
    private readonly favoriteMealService = inject(FavoriteMealService);

    public readonly isFavorite = signal(false);
    public readonly isFavoriteLoading = signal(false);
    public readonly favoriteIcon = computed(() => (this.isFavorite() ? 'star' : 'star_border'));
    public readonly favoriteAriaLabelKey = computed(() =>
        this.isFavorite() ? 'CONSUMPTION_DETAIL.REMOVE_FAVORITE' : 'CONSUMPTION_DETAIL.ADD_FAVORITE',
    );

    private initialFavoriteState = false;
    private favoriteMealId: string | null = null;

    public initialize(meal: Meal): void {
        this.initialFavoriteState = meal.isFavorite ?? false;
        this.isFavorite.set(this.initialFavoriteState);
        this.favoriteMealId = meal.favoriteMealId ?? null;

        this.favoriteMealService.isFavorite(meal.id).subscribe(isFavorite => {
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
        const formattedDate = this.datePipe.transform(meal.date, 'dd.MM.yyyy');
        const data: ConfirmDeleteDialogData = {
            title: this.translate.instant('CONFIRM_DELETE.TITLE', {
                type: this.translate.instant('CONSUMPTION_DETAIL.ENTITY_NAME'),
            }),
            message: this.translate.instant('CONFIRM_DELETE.MESSAGE', { name: formattedDate ?? '' }),
            name: formattedDate ?? '',
            entityType: this.translate.instant('CONSUMPTION_DETAIL.ENTITY_NAME'),
            confirmLabel: this.translate.instant('CONFIRM_DELETE.CONFIRM'),
            cancelLabel: this.translate.instant('CONFIRM_DELETE.CANCEL'),
        };

        this.fdDialogService
            .open(ConfirmDeleteDialogComponent, { data, size: 'sm' })
            .afterClosed()
            .subscribe(confirm => {
                if (confirm === true) {
                    this.dialogRef.close(new MealDetailActionResult(meal.id, 'Delete', this.hasFavoriteChanged()));
                }
            });
    }

    public toggleFavorite(meal: Meal): void {
        if (this.isFavoriteLoading()) {
            return;
        }

        this.isFavoriteLoading.set(true);

        if (this.isFavorite()) {
            this.removeFavorite(meal);
            return;
        }

        this.favoriteMealService.add(meal.id).subscribe({
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
        if (this.favoriteMealId !== null && this.favoriteMealId.length > 0) {
            this.favoriteMealService.remove(this.favoriteMealId).subscribe({
                next: () => {
                    this.isFavorite.set(false);
                    this.favoriteMealId = null;
                    this.isFavoriteLoading.set(false);
                },
                error: () => {
                    this.isFavoriteLoading.set(false);
                },
            });
            return;
        }

        this.favoriteMealService
            .getAll()
            .pipe(
                switchMap(favorites => {
                    const match = favorites.find(favorite => favorite.mealId === meal.id);
                    return match === undefined ? of(null) : this.favoriteMealService.remove(match.id);
                }),
            )
            .subscribe({
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

    private hasFavoriteChanged(): boolean {
        return this.initialFavoriteState !== this.isFavorite();
    }
}
