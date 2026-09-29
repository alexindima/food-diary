import { DestroyRef, effect, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { AuthService } from '../../../services/auth.service';
import { PublicAuthDialogService } from '../../public/lib/public-auth-dialog.service';
import { FavoriteRecipeService } from '../api/favorite-recipe.service';
import type { PublicRecipe } from '../models/public-recipe.data';

@Injectable()
export class PublicCatalogFavorites {
    private readonly api = inject(FavoriteRecipeService);
    private readonly auth = inject(AuthService);
    private readonly dialog = inject(PublicAuthDialogService);
    private readonly router = inject(Router);
    private readonly destroyRef = inject(DestroyRef);
    private ids = new Map<string, string>();
    private session = 0;
    private ready: Promise<void> | undefined;
    public readonly savedIds = signal<ReadonlySet<string>>(new Set());
    public readonly busyIds = signal<ReadonlySet<string>>(new Set());
    public readonly message = signal<string | null>(null);

    public constructor() {
        effect(() => {
            const authenticated = this.auth.isAuthenticated();
            const session = ++this.session;
            this.ids = new Map();
            this.savedIds.set(new Set());
            this.message.set(null);
            this.ready = authenticated ? this.loadAsync(session) : undefined;
        });
    }

    public async toggleAsync(recipe: PublicRecipe): Promise<void> {
        if (this.busyIds().has(recipe.id)) {
            return;
        }
        this.busyIds.update(ids => new Set([...ids, recipe.id]));
        this.message.set(null);
        try {
            if (!(await this.authenticateAsync())) {
                return;
            }
            await this.ready;
            const session = this.session;
            if (this.destroyRef.destroyed || !this.auth.isAuthenticated()) {
                return;
            }
            await this.changeAsync(recipe, session);
        } catch {
            if (!this.destroyRef.destroyed) {
                this.message.set('PUBLIC_RECIPES.ACTION_ERROR');
            }
        } finally {
            this.busyIds.update(ids => new Set([...ids].filter(id => id !== recipe.id)));
        }
    }

    private async authenticateAsync(): Promise<boolean> {
        if (this.auth.isAuthenticated()) {
            return true;
        }
        const ref = await this.dialog.openAsync({
            mode: 'login',
            messageKey: 'PUBLIC_RECIPES.SIGN_IN_TO_SAVE',
            returnUrl: this.router.url,
            destroyRef: this.destroyRef,
        });
        if (ref !== null) {
            await firstValueFrom(ref.afterClosed());
        }
        if (this.destroyRef.destroyed || !this.auth.isAuthenticated()) {
            return false;
        }
        await this.loadAsync(this.session);
        return true;
    }

    private async changeAsync(recipe: PublicRecipe, session: number): Promise<void> {
        const favoriteId = this.ids.get(recipe.id);
        if (favoriteId !== undefined) {
            await firstValueFrom(this.api.remove(favoriteId));
            if (!this.isCurrent(session)) {
                return;
            }
            this.ids.delete(recipe.id);
        } else {
            const favorite = await firstValueFrom(this.api.add(recipe.id, recipe.name));
            if (!this.isCurrent(session)) {
                return;
            }
            this.ids.set(recipe.id, favorite.id);
        }
        this.savedIds.set(new Set(this.ids.keys()));
        this.message.set(favoriteId === undefined ? 'PUBLIC_RECIPES.SAVED' : 'PUBLIC_RECIPES.REMOVED');
    }

    private isCurrent(session: number): boolean {
        return session === this.session && !this.destroyRef.destroyed && this.auth.isAuthenticated();
    }

    private async loadAsync(session: number): Promise<void> {
        const favorites = await firstValueFrom(this.api.getLookupPage());
        if (session !== this.session || this.destroyRef.destroyed || !this.auth.isAuthenticated()) {
            return;
        }
        this.ids = new Map(favorites.map(favorite => [favorite.recipeId, favorite.id]));
        this.savedIds.set(new Set(this.ids.keys()));
    }
}
