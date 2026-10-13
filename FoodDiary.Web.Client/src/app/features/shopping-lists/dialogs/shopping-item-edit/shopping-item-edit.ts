import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { disabled, form, FormField, FormRoot, maxLength, required, validate } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiDialogComponent, FdUiInputComponent, FdUiSelectComponent } from 'fd-ui-kit';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';

import type { ShoppingListDraftItem } from '../../lib/shopping-list.facade';
import { isShoppingListAmountValid } from '../../lib/shopping-list-amount.validation';
import { buildShoppingListUnitOptions, normalizeShoppingListUnit, shoppingListCategoryLabel } from '../../lib/shopping-list-item.mapper';

const ITEM_NAME_LIMIT = 256;
const CATEGORY_LIMIT = 128;
const NOTE_LIMIT = 512;

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
    private readonly originalCategoryLabel = shoppingListCategoryLabel(this.data.category, this.data.productId, key =>
        this.translate.instant(key),
    );
    protected readonly draft = signal<ShoppingItemEditData & { category: string; note: string }>({
        ...this.data,
        unit: normalizeShoppingListUnit(this.data.unit),
        category: this.originalCategoryLabel,
        note: this.data.note ?? '',
    });
    protected readonly fields = form(this.draft, path => {
        required(path.name);
        maxLength(path.name, ITEM_NAME_LIMIT);
        maxLength(path.category, CATEGORY_LIMIT);
        maxLength(path.note, NOTE_LIMIT);
        disabled(path.unit, { when: () => this.isProductLinked });
        validate(path.amount, ({ value }) =>
            isShoppingListAmountValid(value())
                ? undefined
                : { kind: 'amount', message: this.translate.instant('SHOPPING_LIST.AMOUNT_ERROR') },
        );
    });
    protected readonly units = buildShoppingListUnitOptions(key => this.translate.instant(key));
    protected readonly valid = computed(() => {
        return this.draft().name.trim().length > 0 && !this.fields().invalid();
    });
    protected close(): void {
        this.ref.close();
    }
    protected save(event: Event): void {
        event.preventDefault();
        if (this.valid()) {
            const draft = this.draft();
            this.ref.close({ ...draft, category: draft.category === this.originalCategoryLabel ? this.data.category : draft.category });
        }
    }
}
