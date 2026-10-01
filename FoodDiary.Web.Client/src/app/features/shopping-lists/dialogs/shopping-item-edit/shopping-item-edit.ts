import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { disabled, form, FormField, FormRoot, maxLength, required } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiDialogComponent, FdUiInputComponent, FdUiSelectComponent } from 'fd-ui-kit';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';

import type { ShoppingListDraftItem } from '../../lib/shopping-list.facade';
import { buildShoppingListUnitOptions } from '../../lib/shopping-list-item.mapper';

const ITEM_NAME_LIMIT = 256;
const CATEGORY_LIMIT = 128;
const NOTE_LIMIT = 512;
const AMOUNT_LIMIT = 1_000_000;

export type ShoppingItemEditData = ShoppingListDraftItem & { productId?: string | null };

@Component({
    selector: 'fd-shopping-item-edit',
    templateUrl: './shopping-item-edit.html',
    imports: [TranslatePipe, FormField, FormRoot, FdUiDialogComponent, FdUiInputComponent, FdUiSelectComponent, FdUiButtonComponent],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShoppingItemEditComponent {
    private readonly data = inject<ShoppingItemEditData>(FD_UI_DIALOG_DATA);
    private readonly ref = inject(FdUiDialogRef<ShoppingItemEditComponent, ShoppingListDraftItem>);
    private readonly translate = inject(TranslateService);
    protected readonly isProductLinked = Boolean(this.data.productId);
    protected readonly draft = signal({ ...this.data, category: this.data.category ?? '', note: this.data.note ?? '' });
    protected readonly fields = form(this.draft, path => {
        required(path.name);
        maxLength(path.name, ITEM_NAME_LIMIT);
        maxLength(path.category, CATEGORY_LIMIT);
        maxLength(path.note, NOTE_LIMIT);
        disabled(path.unit, { when: () => this.isProductLinked });
    });
    protected readonly units = buildShoppingListUnitOptions(key => this.translate.instant(key));
    protected readonly valid = computed(() => {
        const { name, amount } = this.draft();
        return (
            name.trim().length > 0 &&
            !this.fields().invalid() &&
            (amount === null || (Number.isFinite(Number(amount)) && Number(amount) > 0 && Number(amount) <= AMOUNT_LIMIT))
        );
    });
    protected close(): void {
        this.ref.close();
    }
    protected save(event: Event): void {
        event.preventDefault();
        if (this.valid()) {
            this.ref.close(this.draft());
        }
    }
}
