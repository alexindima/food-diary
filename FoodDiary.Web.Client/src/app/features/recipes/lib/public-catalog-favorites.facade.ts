import { DestroyRef, effect, inject, Injectable, signal, untracked } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { finalize, firstValueFrom, map, Subject, tap } from 'rxjs';

import { AuthService } from '../../../services/auth.service';
import type { FavoriteRecipe } from '../../../shared/models/recipe.data';
import { entityId } from '../../../shared/models/semantics/entity-id';
import { PublicAuthDialogService } from '../../public/contracts/auth-dialog';
import { FavoriteRecipeService } from '../api/favorite-recipe.service';
import type { PublicRecipe } from '../models/public-recipe.data';

type FavoriteLookup = { session: number; finished: Subject<void>; ready: Promise<void> };

@Injectable()
export class PublicCatalogFavorites {
    private readonly api = inject(FavoriteRecipeService);
    private readonly auth = inject(AuthService);
    private readonly dialog = inject(PublicAuthDialogService);
    private readonly router = inject(Router);
    private readonly destroyRef = inject(DestroyRef);
    private readonly requestedLookup = signal<FavoriteLookup | undefined>(undefined);
    private readonly lookup = rxResource({
        params: this.requestedLookup,
        stream: ({ params }) =>
            this.api.getLookupPage().pipe(
                tap({
                    next: favorites => {
                        this.applyLookup(favorites, params.session);
                        params.finished.next();
                    },
                    error: (error: unknown) => {
                        params.finished.error(error);
                    },
                }),
                finalize(() => {
                    params.finished.complete();
                }),
            ),
    });
    private ids = new Map<string, string>();
    private session = 0;
    private ownerId: string | null = null;
    private ready: Promise<void> | undefined;
    public readonly savedIds = signal<ReadonlySet<string>>(new Set());
    public readonly busyIds = signal<ReadonlySet<string>>(new Set());
    public readonly message = signal<string | null>(null);

    public constructor() {
        effect(() => {
            const authenticated = this.auth.isAuthenticated();
            const ownerId = this.auth.getUserId();
            untracked(() => {
                this.bindOwner(authenticated ? ownerId : null);
            });
        });
        this.destroyRef.onDestroy(() => {
            this.requestedLookup()?.finished.complete();
        });
    }

    private bindOwner(ownerId: string | null): void {
        if (this.ownerId === ownerId) {
            return;
        }
        const session = ++this.session;
        this.ownerId = ownerId;
        this.ids = new Map();
        this.savedIds.set(new Set());
        this.busyIds.set(new Set());
        this.message.set(null);

        const previous = this.requestedLookup();
        this.requestedLookup.set(undefined);
        this.lookup.set(undefined);
        previous?.finished.complete();
        this.ready = undefined;
        if (ownerId !== null) {
            this.requestLookup(session);
        }
    }

    private requestLookup(session: number): FavoriteLookup {
        const finished = new Subject<void>();
        const ready = firstValueFrom(finished, { defaultValue: undefined });
        const lookup = { session, finished, ready };
        this.ready = ready;
        this.requestedLookup.set(lookup);
        void ready.catch(() => {
            if (this.isCurrent(session) && this.ready === ready) {
                this.ready = undefined;
                this.message.set('PUBLIC_RECIPES.ACTION_ERROR');
            }
        });
        return lookup;
    }

    public async toggleAsync(recipe: PublicRecipe): Promise<void> {
        if (this.busyIds().has(recipe.id)) {
            return;
        }
        this.busyIds.update(ids => new Set([...ids, recipe.id]));
        this.message.set(null);
        let session = this.session;
        let ready: Promise<void> | undefined;
        try {
            const authenticatedSession = await this.authenticateAsync();
            if (authenticatedSession === null) {
                return;
            }
            session = authenticatedSession;
            if (!this.isCurrent(session)) {
                return;
            }
            this.busyIds.update(ids => new Set([...ids, recipe.id]));
            ready = this.ready ?? this.requestLookup(session).ready;
            await ready;
            if (!this.isCurrent(session)) {
                return;
            }
            await this.changeAsync(recipe, session);
        } catch {
            this.showToggleError(session, ready);
        } finally {
            if (this.isCurrentSession(session)) {
                this.busyIds.update(ids => new Set([...ids].filter(id => id !== recipe.id)));
            }
        }
    }

    private showToggleError(session: number, ready: Promise<void> | undefined): void {
        const isCurrent = ready === undefined ? this.isCurrentSession(session) : this.isCurrent(session);
        if (!isCurrent) {
            return;
        }
        if (this.ready === ready) {
            this.ready = undefined;
        }
        this.message.set('PUBLIC_RECIPES.ACTION_ERROR');
    }

    private async authenticateAsync(): Promise<number | null> {
        if (this.auth.isAuthenticated()) {
            return this.bindAuthenticatedOwner();
        }
        const ref = await this.dialog.openAsync({
            mode: 'login',
            messageKey: 'PUBLIC_RECIPES.SIGN_IN_TO_SAVE',
            returnUrl: this.router.url,
            destroyRef: this.destroyRef,
        });
        return ref === null
            ? this.bindAuthenticatedOwner()
            : firstValueFrom(ref.afterClosed().pipe(map(() => this.bindAuthenticatedOwner())));
    }

    private bindAuthenticatedOwner(): number | null {
        if (this.destroyRef.destroyed || !this.auth.isAuthenticated()) {
            return null;
        }
        const ownerId = this.auth.getUserId();
        if (ownerId === null) {
            return null;
        }
        this.bindOwner(ownerId);
        return this.session;
    }

    private async changeAsync(recipe: PublicRecipe, session: number): Promise<void> {
        const favoriteId = this.ids.get(recipe.id);
        if (favoriteId !== undefined) {
            await firstValueFrom(this.api.remove(entityId<'favorite-recipe'>(favoriteId)));
            if (!this.isCurrent(session)) {
                return;
            }
            this.ids.delete(recipe.id);
        } else {
            const favorite = await firstValueFrom(this.api.add(entityId<'recipe'>(recipe.id), recipe.name));
            if (!this.isCurrent(session)) {
                return;
            }
            this.ids.set(recipe.id, favorite.id);
        }
        this.savedIds.set(new Set(this.ids.keys()));
        this.message.set(favoriteId === undefined ? 'PUBLIC_RECIPES.SAVED' : 'PUBLIC_RECIPES.REMOVED');
    }

    private isCurrent(session: number): boolean {
        return (
            this.isCurrentSession(session) && this.ownerId !== null && this.ownerId === this.auth.getUserId() && this.auth.isAuthenticated()
        );
    }

    private isCurrentSession(session: number): boolean {
        return session === this.session && !this.destroyRef.destroyed;
    }

    private applyLookup(favorites: readonly FavoriteRecipe[], session: number): void {
        if (!this.isCurrent(session)) {
            return;
        }
        this.ids = new Map(favorites.map(favorite => [favorite.recipeId, favorite.id]));
        this.savedIds.set(new Set(this.ids.keys()));
    }
}
