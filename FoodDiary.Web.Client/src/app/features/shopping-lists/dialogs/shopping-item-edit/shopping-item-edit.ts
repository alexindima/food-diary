import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { disabled, form, FormField, maxLength, validate } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiDialogComponent, FdUiInputComponent, FdUiSelectComponent } from 'fd-ui-kit';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiFormDirective } from 'fd-ui-kit/form/fd-ui-form.directive';

import type { ShoppingListDraftItem } from '../../lib/shopping-list.facade';
import { isShoppingListAmountValid } from '../../lib/shopping-list-amount.validation';
import { SHOPPING_ITEM_CATEGORY_LIMIT, SHOPPING_ITEM_NAME_LIMIT, SHOPPING_ITEM_NOTE_LIMIT } from '../../lib/shopping-list-input.constants';
import { buildShoppingListUnitOptions, normalizeShoppingListUnit, shoppingListCategoryLabel } from '../../lib/shopping-list-item.mapper';

export type ShoppingItemEditData = ShoppingListDraftItem & { productId?: string | null };

@Component({
    selector: 'fd-shopping-item-edit',
    templateUrl: './shopping-item-edit.html',
    imports: [
        TranslatePipe,
        FormField,
        FdUiFormDirective,
        FdUiDialogComponent,
        FdUiInputComponent,
        FdUiSelectComponent,
        FdUiButtonComponent,
    ],
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
        validate(path.name, ({ value }) => (value().trim().length === 0 ? { kind: 'required' } : undefined));
        maxLength(path.name, SHOPPING_ITEM_NAME_LIMIT);
        maxLength(path.category, SHOPPING_ITEM_CATEGORY_LIMIT);
        maxLength(path.note, SHOPPING_ITEM_NOTE_LIMIT);
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
