import { DestroyRef, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { of, switchMap, take } from 'rxjs';

import {
    ConfirmDeleteDialogComponent,
    type ConfirmDeleteDialogData,
} from '../../../../../components/shared/confirm-delete-dialog/confirm-delete-dialog.component';
import { FavoriteRecipeService } from '../../../api/favorite-recipe.service';
import { RecipeService } from '../../../api/recipe.service';
import type { Recipe } from '../../../models/recipe.data';
import { RecipeDetailActionResult } from './recipe-detail.types';

@Injectable({ providedIn: 'root' })
export class RecipeDetailFacade {
    private readonly recipeService = inject(RecipeService);
    private readonly favoriteRecipeService = inject(FavoriteRecipeService);
    private readonly dialogRef = inject(FdUiDialogRef<unknown, RecipeDetailActionResult>);
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);

    public readonly isFavorite = signal(false);
    public readonly isFavoriteLoading = signal(false);
    public readonly isDuplicateInProgress = signal(false);

    private initialFavoriteState = false;
    private favoriteRecipeId: string | null = null;

    public initialize(recipe: Recipe): void {
        this.initialFavoriteState = recipe.isFavorite ?? false;
        this.isFavorite.set(this.initialFavoriteState);
        this.favoriteRecipeId = recipe.favoriteRecipeId ?? null;

        this.favoriteRecipeService
            .isFavorite(recipe.id)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(isFav => {
                this.initialFavoriteState = isFav;
                this.isFavorite.set(isFav);
            });
    }

    public close(recipe: Recipe): void {
        if (this.hasFavoriteChanged()) {
            this.dialogRef.close(new RecipeDetailActionResult(recipe.id, 'FavoriteChanged', true));
            return;
        }

        this.dialogRef.close();
    }

    public edit(recipe: Recipe): void {
        this.dialogRef.close(new RecipeDetailActionResult(recipe.id, 'Edit', this.hasFavoriteChanged()));
    }

    public delete(recipe: Recipe): void {
        const data = this.buildConfirmDeleteData(recipe);

        this.fdDialogService
            .open(ConfirmDeleteDialogComponent, { size: 'sm', data })
            .afterClosed()
            .pipe(take(1))
            .subscribe(confirm => {
                if (confirm === true) {
                    this.dialogRef.close(new RecipeDetailActionResult(recipe.id, 'Delete', this.hasFavoriteChanged()));
                }
            });
    }

    public duplicate(recipe: Recipe): void {
        if (this.isDuplicateInProgress()) {
            return;
        }

        this.isDuplicateInProgress.set(true);
        this.recipeService
            .duplicate(recipe.id)
            .pipe(take(1))
            .subscribe({
                next: duplicated => {
                    this.dialogRef.close(new RecipeDetailActionResult(duplicated.id, 'Duplicate', this.hasFavoriteChanged()));
                },
                error: () => {
                    this.isDuplicateInProgress.set(false);
                },
            });
    }

    public toggleFavorite(recipe: Recipe): void {
        if (this.isFavoriteLoading()) {
            return;
        }

        this.isFavoriteLoading.set(true);

        if (this.isFavorite()) {
            this.removeFavorite(recipe);
            return;
        }

        this.favoriteRecipeService
            .add(recipe.id)
            .pipe(take(1))
            .subscribe({
                next: favorite => {
                    this.isFavorite.set(true);
                    this.favoriteRecipeId = favorite.id;
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

    private removeFavorite(recipe: Recipe): void {
        const favoriteId = this.favoriteRecipeId;
        const request$ =
            favoriteId !== null && favoriteId.length > 0
                ? this.favoriteRecipeService.remove(favoriteId)
                : this.favoriteRecipeService.getAll().pipe(
                      switchMap(favorites => {
                          const match = favorites.find(favorite => favorite.recipeId === recipe.id);
                          return match === undefined ? of(null) : this.favoriteRecipeService.remove(match.id);
                      }),
                  );

        request$.pipe(take(1)).subscribe({
            next: () => {
                this.isFavorite.set(false);
                this.favoriteRecipeId = null;
                this.isFavoriteLoading.set(false);
            },
            error: () => {
                this.isFavoriteLoading.set(false);
            },
        });
    }

    private buildConfirmDeleteData(recipe: Recipe): ConfirmDeleteDialogData {
        return {
            title: this.translateService.instant('CONFIRM_DELETE.TITLE', {
                type: this.translateService.instant('RECIPE_DETAIL.ENTITY_NAME'),
            }),
            message: this.translateService.instant('CONFIRM_DELETE.MESSAGE', { name: recipe.name }),
            name: recipe.name,
            entityType: this.translateService.instant('RECIPE_DETAIL.ENTITY_NAME'),
            confirmLabel: this.translateService.instant('CONFIRM_DELETE.CONFIRM'),
            cancelLabel: this.translateService.instant('CONFIRM_DELETE.CANCEL'),
        };
    }
}
