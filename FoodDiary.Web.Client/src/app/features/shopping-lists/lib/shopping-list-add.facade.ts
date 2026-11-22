import { DestroyRef, effect, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { firstValueFrom } from 'rxjs';

import { AuthService } from '../../../services/auth.service';
import { SessionEventsService } from '../../../shared/auth/session-events.service';
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
    private readonly sessionEvents = inject(SessionEventsService);
    private scope = '';
    private scopeRevision = 0;
    private name = '';
    private closePicker: (() => void) | undefined;
    private readonly added = signal<ReadonlySet<string>>(new Set());
    public readonly target = signal<ShoppingListTarget | null>(null);
    public readonly busy = signal(false);
    public readonly message = signal<string | null>(null);

    public constructor() {
        this.sessionEvents.sessionEnded$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.replaceScope();
        });
        this.destroyRef.onDestroy(() => {
            this.closePicker?.();
        });
        effect(() => {
            if (!this.auth.isAuthenticated()) {
                this.reset();
            }
        });
    }
    public setScope(scope: string, name: string): void {
        if (scope !== this.scope) {
            this.scope = scope;
            this.replaceScope();
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
            this.finishOperation(scopeRevision);
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
            const target = await this.prepareAsync(scopeRevision, remaining[0].item, remaining.length, confirm);
            if (target === null || !this.isCurrent(scopeRevision)) {
                return;
            }
            const pending = remaining.filter(entry => !this.isAdded(entry.index));
            if (pending.length === 0) {
                return;
            }
            const list = await this.saveAsync(
                scopeRevision,
                target,
                pending.map(entry => entry.item),
            );
            this.applySavedAddition(scopeRevision, list, pending);
        } catch {
            this.message.update(message => (this.isCurrent(scopeRevision) ? 'PUBLIC_RECIPES.SHOPPING_ERROR' : message));
        } finally {
            this.finishOperation(scopeRevision);
        }
    }
    private applySavedAddition(scopeRevision: number, list: ShoppingList | null, entries: ReadonlyArray<{ index: number }>): void {
        if (list === null || !this.isCurrent(scopeRevision)) {
            return;
        }
        this.target.set({ id: list.id, name: list.name });
        this.added.update(keys => new Set([...keys, ...entries.map(entry => `${list.id}:${entry.index}`)]));
        this.message.set('PUBLIC_RECIPES.SHOPPING_SUCCESS');
    }
    private async saveAsync(scopeRevision: number, target: ShoppingListTarget, items: ShoppingListItemDto[]): Promise<ShoppingList | null> {
        return target.id === null
            ? firstValueFrom(this.api.create({ name: target.name, items: appendShoppingItems([], items) }))
            : this.appendAsync(scopeRevision, entityId<'shopping-list'>(target.id), items);
    }
    private async prepareAsync(
        scopeRevision: number,
        item: ShoppingListItemDto,
        count: number,
        confirm: boolean,
    ): Promise<ShoppingListTarget | null> {
        await this.authenticateAsync();
        if (!this.isCurrent(scopeRevision)) {
            return null;
        }
        return confirm
            ? this.chooseTargetAsync(scopeRevision, undefined, count)
            : (this.target() ?? this.chooseTargetAsync(scopeRevision, item));
    }
    private async appendAsync(scopeRevision: number, id: ShoppingListId, items: ShoppingListItemDto[]): Promise<ShoppingList | null> {
        const list = await firstValueFrom(this.api.getById(id));
        if (!this.isCurrent(scopeRevision)) {
            return null;
        }
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
    private async chooseTargetAsync(scopeRevision: number, item?: ShoppingListItemDto, count?: number): Promise<ShoppingListTarget | null> {
        const lists = await firstValueFrom(this.api.getSelectionPage());
        const { ShoppingListPickerComponent } = await import('../dialogs/shopping-list-picker/shopping-list-picker');
        if (!this.isCurrent(scopeRevision)) {
            return null;
        }
        const ref = this.dialogs.open<InstanceType<typeof ShoppingListPickerComponent>, ShoppingListPickerData, ShoppingListTarget>(
            ShoppingListPickerComponent,
            {
                preset: 'form',
                autoFocus: (this.target()?.id ?? null) === null ? 'fd-ui-input input' : 'first-tabbable',
                data: { lists, name: this.name, selected: this.target(), item, count },
            },
        );
        this.closePicker = (): void => {
            this.closePicker = undefined;
            ref.close();
        };
        try {
            const target = await firstValueFrom(ref.afterClosed());
            if (target === undefined || !this.isCurrent(scopeRevision)) {
                return null;
            }
            this.target.set(target);
            return target;
        } finally {
            if (scopeRevision === this.scopeRevision) {
                this.closePicker = undefined;
            }
        }
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

    private replaceScope(): void {
        this.scopeRevision += 1;
        this.reset();
        this.busy.set(false);
    }

    private finishOperation(scopeRevision: number): void {
        if (scopeRevision === this.scopeRevision) {
            this.busy.set(false);
        }
    }

    private reset(): void {
        this.closePicker?.();
        this.target.set(null);
        this.added.set(new Set());
        this.message.set(null);
    }
}
