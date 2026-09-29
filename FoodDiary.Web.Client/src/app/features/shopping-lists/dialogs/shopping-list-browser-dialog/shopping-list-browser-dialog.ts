import { ChangeDetectionStrategy, Component, inject, type Signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiDialogComponent } from 'fd-ui-kit';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';

import { ShoppingListBrowserComponent } from '../../components/shopping-list-browser/shopping-list-browser';
import type { ShoppingListPage, ShoppingListSummary } from '../../models/shopping-list.data';
export type ShoppingListBrowserResult = { id: string } | { create: true; name?: string | void };
export type ShoppingListBrowserData = {
    lists: Signal<readonly ShoppingListSummary[]>;
    initialPage: Signal<ShoppingListPage | null>;
    selectedId: string | null;
};
@Component({
    selector: 'fd-shopping-list-browser-dialog',
    imports: [TranslatePipe, FdUiDialogComponent, ShoppingListBrowserComponent],
    templateUrl: './shopping-list-browser-dialog.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShoppingListBrowserDialogComponent {
    protected readonly data = inject<ShoppingListBrowserData>(FD_UI_DIALOG_DATA);
    private readonly ref = inject(FdUiDialogRef<ShoppingListBrowserDialogComponent, ShoppingListBrowserResult>);
    protected create(name: string | void): void {
        this.ref.close({ create: true, name });
    }
    protected close(id?: string): void {
        this.ref.close(id === undefined ? undefined : { id });
    }
}
