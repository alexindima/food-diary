import { DestroyRef, effect, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { firstValueFrom } from 'rxjs';

import { AuthService } from '../../../services/auth.service';
import type { ShoppingListId } from '../../../shared/models/semantics/entity-id';
import { entityId } from '../../../shared/models/semantics/entity-id';
import type { ShoppingList, ShoppingListItemDto } from '../../../shared/models/shopping-list.data';
import { PublicAuthDialogService } from '../../public/contracts/auth-dialog';
import { ShoppingListService } from '../api/shopping-list.service';
import type { ShoppingListPickerData, ShoppingListTarget } from '../dialogs/shopping-list-picker/shopping-list-picker';
import { mapShoppingListItemToDto } from './shopping-list-item.mapper';
import { appendShoppingItems } from './shopping-list-merge';

@Injectable()
export class ShoppingListAddFacade {
    private readonly api = inject(ShoppingListService);
    private readonly dialogs = inject(FdUiDialogService);
    private readonly auth = inject(AuthService);
    private readonly authDialog = inject(PublicAuthDialogService);
    private readonly router = inject(Router);
    private readonly destroyRef = inject(DestroyRef);
    private scope = '';
    private scopeRevision = 0;
    private name = '';
    private readonly added = signal<ReadonlySet<string>>(new Set());
    public readonly target = signal<ShoppingListTarget | null>(null);
    public readonly busy = signal(false);
    public readonly message = signal<string | null>(null);

    public constructor() {
        effect(() => {
            if (!this.auth.isAuthenticated()) {
                this.reset();
            }
        });
    }
    public setScope(scope: string, name: string): void {
        if (scope !== this.scope) {
            this.scope = scope;
            this.scopeRevision += 1;
            this.reset();
        }
        this.name = name;
    }
    public isAdded(key: number): boolean {
        return this.added().has(`${this.target()?.id}:${key}`);
    }
    public async chooseAsync(): Promise<void> {
        if (this.busy()) {
            return;
        }
        this.busy.set(true);
        this.message.set(null);
        const scopeRevision = this.scopeRevision;
        try {
            await this.authenticateAsync();
            if (this.isCurrent(scopeRevision)) {
                await this.chooseTargetAsync(scopeRevision);
            }
        } catch {
            if (this.isCurrent(scopeRevision)) {
                this.message.set('PUBLIC_RECIPES.SHOPPING_ERROR');
            }
        } finally {
            this.busy.set(false);
        }
    }
    public async addAsync(key: number, item: ShoppingListItemDto): Promise<void> {
        await this.addItemsAsync([{ index: key, item }]);
    }
    public async addAllAsync(entries: Array<{ index: number; item: ShoppingListItemDto }>): Promise<void> {
        await this.addItemsAsync(entries, true);
    }
    private async addItemsAsync(entries: Array<{ index: number; item: ShoppingListItemDto }>, confirm = false): Promise<void> {
        const remaining = entries.filter(entry => !this.isAdded(entry.index));
        if (this.busy() || remaining.length === 0) {
            return;
        }
        this.busy.set(true);
        this.message.set(null);
        const scopeRevision = this.scopeRevision;
        try {
            if (!(await this.prepareAsync(scopeRevision, remaining[0].item, remaining.length, confirm)) || !this.isCurrent(scopeRevision)) {
                return;
            }
            const target = this.target();
            if (target === null) {
                return;
            }
            const pending = remaining.filter(entry => !this.isAdded(entry.index));
            if (pending.length === 0) {
                return;
            }
            const list = await this.saveAsync(
                target,
                pending.map(entry => entry.item),
            );
            if (!this.isCurrent(scopeRevision)) {
                return;
            }
            this.target.set({ id: list.id, name: list.name });
            this.added.update(keys => new Set([...keys, ...pending.map(entry => `${list.id}:${entry.index}`)]));
            this.message.set('PUBLIC_RECIPES.SHOPPING_SUCCESS');
        } catch {
            this.message.update(message => (this.isCurrent(scopeRevision) ? 'PUBLIC_RECIPES.SHOPPING_ERROR' : message));
        } finally {
            this.busy.set(false);
        }
    }
    private async saveAsync(target: ShoppingListTarget, items: ShoppingListItemDto[]): Promise<ShoppingList> {
        return target.id === null
            ? firstValueFrom(this.api.create({ name: target.name, items: appendShoppingItems([], items) }))
            : this.appendAsync(entityId<'shopping-list'>(target.id), items);
    }
    private async prepareAsync(scopeRevision: number, item: ShoppingListItemDto, count: number, confirm: boolean): Promise<boolean> {
        await this.authenticateAsync();
        if (!this.isCurrent(scopeRevision)) {
            return false;
        }
        return confirm
            ? this.chooseTargetAsync(scopeRevision, undefined, count)
            : this.target() !== null || this.chooseTargetAsync(scopeRevision, item);
    }
    private async appendAsync(id: ShoppingListId, items: ShoppingListItemDto[]): Promise<ShoppingList> {
        const list = await firstValueFrom(this.api.getById(id));
        if (list === null) {
            this.target.set(null);
            throw new Error('Shopping list unavailable');
        }
        return firstValueFrom(
            this.api.update(id, {
                expectedRevision: list.revision,
                items: appendShoppingItems(list.items.map(mapShoppingListItemToDto), items),
            }),
        );
    }
    private async chooseTargetAsync(scopeRevision: number, item?: ShoppingListItemDto, count?: number): Promise<boolean> {
        const lists = await firstValueFrom(this.api.getSelectionPage());
        const { ShoppingListPickerComponent } = await import('../dialogs/shopping-list-picker/shopping-list-picker');
        if (!this.isCurrent(scopeRevision)) {
            return false;
        }
        const ref = this.dialogs.open<InstanceType<typeof ShoppingListPickerComponent>, ShoppingListPickerData, ShoppingListTarget>(
            ShoppingListPickerComponent,
            {
                preset: 'form',
                autoFocus: (this.target()?.id ?? null) === null ? 'fd-ui-input input' : 'first-tabbable',
                data: { lists, name: this.name, selected: this.target(), item, count },
            },
        );
        const target = await firstValueFrom(ref.afterClosed());
        if (target === undefined || !this.isCurrent(scopeRevision)) {
            return false;
        }
        this.target.set(target);
        return true;
    }
    private async authenticateAsync(): Promise<void> {
        if (!this.auth.isAuthenticated()) {
            const ref = await this.authDialog.openAsync({ mode: 'login', returnUrl: this.router.url, destroyRef: this.destroyRef });
            if (ref !== null) {
                await firstValueFrom(ref.afterClosed());
            }
        }
    }
    private isCurrent(scopeRevision: number): boolean {
        return scopeRevision === this.scopeRevision && !this.destroyRef.destroyed && this.auth.isAuthenticated();
    }
    private reset(): void {
        this.target.set(null);
        this.added.set(new Set());
        this.message.set(null);
    }
}
