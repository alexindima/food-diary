import { computed, DestroyRef, inject, Injectable, signal } from '@angular/core';
import { rxResource, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { catchError, map, of, switchMap, take } from 'rxjs';

import {
    ConfirmDeleteDialogComponent,
    type ConfirmDeleteDialogData,
} from '../../../../components/shared/confirm-delete-dialog/confirm-delete-dialog';
import { type Recipe, type RecipePublicationStatus, RecipeVisibility } from '../../../../shared/models/recipe.data';
import { entityId } from '../../../../shared/models/semantics/entity-id';
import { FavoriteRecipeService } from '../../api/favorite-recipe.service';
import { RecipeService } from '../../api/recipe.service';
import { RecipeDetailActionResult } from '../../components/detail/recipe-detail-lib/recipe-detail.types';

@Injectable()
export class RecipeDetailFacade {
    private readonly recipeService = inject(RecipeService);
    private readonly favoriteRecipeService = inject(FavoriteRecipeService);
    private readonly dialogRef = inject(FdUiDialogRef<unknown, RecipeDetailActionResult>);
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly requestedPublication = signal<{ id: Recipe['id'] } | undefined>(undefined);
    private readonly publication = rxResource({
        params: this.requestedPublication,
        stream: ({ params, abortSignal }) =>
            this.recipeService.getById(params.id, false).pipe(
                take(1),
                map(latest => {
                    if (latest !== null && !abortSignal.aborted && this.requestedPublication() === params) {
                        this.refreshedRecipe.set(latest);
                        this.publicationStatus.set(
                            latest.publicationStatus ?? (latest.visibility === RecipeVisibility.Public ? 'Published' : null),
                        );
                        this.publicationReason.set(latest.publicationReason ?? null);
                    }
                    return latest !== null;
                }),
            ),
    });

    public readonly isFavorite = signal(false);
    public readonly isFavoriteLoading = signal(false);
    public readonly isDuplicateInProgress = signal(false);
    public readonly duplicateError = signal(false);
    public readonly refreshedRecipe = signal<Recipe | null>(null);
    public readonly publicationStatus = signal<RecipePublicationStatus | 'Published' | null>(null);
    public readonly publicationReason = signal<string | null>(null);
    public readonly publicationLoading = this.publication.isLoading;
    public readonly publicationRefreshFailed = computed(
        () => this.publication.error() !== undefined || (this.publication.hasValue() && !this.publication.value()),
    );

    private initialFavoriteState = false;
    private favoriteRecipeId: string | null = null;
    private readonly requestedFavorite = signal<{ id: Recipe['id']; fallback: boolean } | undefined>(undefined);
    private readonly favoriteCheck = rxResource({
        params: this.requestedFavorite,
        stream: ({ params, abortSignal }) =>
            this.favoriteRecipeService.isFavorite(params.id).pipe(
                catchError(() => of(params.fallback)),
                map(isFavorite => {
                    if (!abortSignal.aborted && this.requestedFavorite() === params) {
                        this.initialFavoriteState = isFavorite;
                        this.isFavorite.set(isFavorite);
                    }
                }),
            ),
    });

    public initialize(recipe: Recipe): void {
        this.requestedPublication.set(undefined);
        this.publication.set(undefined);
        this.refreshedRecipe.set(null);
        this.publicationStatus.set(recipe.publicationStatus ?? null);
        this.publicationReason.set(recipe.publicationReason ?? null);
        this.stopFavoriteCheck();
        this.initialFavoriteState = recipe.isFavorite ?? false;
        this.isFavorite.set(this.initialFavoriteState);
        this.favoriteRecipeId = recipe.favoriteRecipeId ?? null;

        this.requestedFavorite.set({ id: recipe.id, fallback: this.initialFavoriteState });
    }

    private stopFavoriteCheck(): void {
        this.requestedFavorite.set(undefined);
        this.favoriteCheck.set(undefined);
    }

    public refreshPublication(recipe: Recipe): void {
        if (this.publicationLoading()) {
            return;
        }
        this.requestedPublication.set({ id: recipe.id });
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
            .pipe(take(1), takeUntilDestroyed(this.destroyRef))
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
        this.duplicateError.set(false);
        this.recipeService
            .duplicate(recipe.id)
            .pipe(take(1), takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: duplicated => {
                    this.dialogRef.close(new RecipeDetailActionResult(duplicated.id, 'Duplicate', this.hasFavoriteChanged()));
                },
                error: () => {
                    this.isDuplicateInProgress.set(false);
                    this.duplicateError.set(true);
                },
            });
    }

    public addToMeal(recipe: Recipe): void {
        this.dialogRef.close(new RecipeDetailActionResult(recipe.id, 'AddToMeal', this.hasFavoriteChanged()));
    }

    public toggleFavorite(recipe: Recipe): void {
        if (this.isFavoriteLoading()) {
            return;
        }

        this.stopFavoriteCheck();
        this.isFavoriteLoading.set(true);

        if (this.isFavorite()) {
            this.removeFavorite(recipe);
            return;
        }

        this.favoriteRecipeService
            .add(recipe.id)
            .pipe(take(1), takeUntilDestroyed(this.destroyRef))
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
                ? this.favoriteRecipeService.remove(entityId<'favorite-recipe'>(favoriteId))
                : this.favoriteRecipeService.getLookupPage().pipe(
                      switchMap(favorites => {
                          const match = favorites.find(favorite => favorite.recipeId === recipe.id);
                          return match === undefined ? of(null) : this.favoriteRecipeService.remove(match.id);
                      }),
                  );

        request$.pipe(take(1), takeUntilDestroyed(this.destroyRef)).subscribe({
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
