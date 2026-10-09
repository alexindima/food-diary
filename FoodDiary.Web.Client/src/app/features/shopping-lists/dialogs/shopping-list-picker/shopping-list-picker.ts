import { afterRenderEffect, ChangeDetectionStrategy, Component, computed, ElementRef, inject, signal, viewChild } from '@angular/core';
import { form, FormField, FormRoot, maxLength } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiDialogComponent, FdUiInputComponent, FdUiSelectComponent } from 'fd-ui-kit';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';

import { injectCurrentLanguage } from '../../../../shared/i18n/inject-current-language';
import { LocalizedNumberPipe } from '../../../../shared/i18n/localized-number.pipe';
import type { ShoppingListId } from '../../../../shared/models/semantics/entity-id';
import type { ShoppingListItemDto, ShoppingListSummary } from '../../../../shared/models/shopping-list.data';

export type ShoppingListTarget = { id: ShoppingListId | null; name: string };
export type ShoppingListPickerData = {
    lists: ShoppingListSummary[];
    name: string;
    selected: ShoppingListTarget | null;
    item?: ShoppingListItemDto;
    count?: number;
};

const LIST_NAME_MAX_LENGTH = 128;

@Component({
    selector: 'fd-shopping-list-picker',
    templateUrl: './shopping-list-picker.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        LocalizedNumberPipe,
        TranslatePipe,
        FormField,
        FormRoot,
        FdUiDialogComponent,
        FdUiInputComponent,
        FdUiSelectComponent,
        FdUiButtonComponent,
    ],
})
export class ShoppingListPickerComponent {
    private readonly data = inject<ShoppingListPickerData>(FD_UI_DIALOG_DATA);
    private readonly ref = inject(FdUiDialogRef<ShoppingListPickerComponent, ShoppingListTarget>);
    private readonly translate = inject(TranslateService);
    private readonly nameField = viewChild<FdUiInputComponent, ElementRef<HTMLElement>>('listName', { read: ElementRef });
    protected readonly language = injectCurrentLanguage();
    protected readonly count = this.data.count;
    protected readonly item = this.data.item;
    protected readonly titleKey =
        this.count !== undefined
            ? 'PUBLIC_RECIPES.SHOPPING_BULK_TITLE'
            : this.item === undefined
              ? 'PUBLIC_RECIPES.SHOPPING_SELECT_TITLE'
              : 'PUBLIC_RECIPES.SHOPPING_PICK';
    protected readonly actionKey = computed(() => {
        if (this.item === undefined && this.count === undefined) {
            return 'PUBLIC_RECIPES.SHOPPING_CHOOSE';
        }
        return this.draft().id === '' ? 'PUBLIC_RECIPES.SHOPPING_CREATE_ADD' : 'PUBLIC_RECIPES.SHOPPING_ADD_CONFIRM';
    });
    public constructor() {
        afterRenderEffect(() => {
            const field = this.nameField();
            field?.nativeElement.querySelector<HTMLInputElement>('input')?.focus();
        });
    }
    protected readonly draft = signal<{ id: string; name: string }>({
        id: this.data.selected?.id ?? '',
        name: (this.data.selected?.name ?? this.data.name).slice(0, LIST_NAME_MAX_LENGTH),
    });
    protected readonly fields = form(this.draft, path => {
        maxLength(path.name, LIST_NAME_MAX_LENGTH);
    });
    protected readonly options = [
        { value: '', label: this.translate.instant('PUBLIC_RECIPES.SHOPPING_CREATE') },
        ...this.data.lists.map(list => ({ value: list.id, label: list.name })),
    ];
    protected readonly valid = computed(() => this.draft().id !== '' || (this.draft().name.trim().length > 0 && !this.fields().invalid()));

    protected submit(event: Event): void {
        event.preventDefault();
        if (!this.valid()) {
            return;
        }
        const draft = this.draft();
        const list = this.data.lists.find(item => item.id === draft.id);
        this.ref.close(list !== undefined ? { id: list.id, name: list.name } : { id: null, name: draft.name.trim() });
    }
    protected close(): void {
        this.ref.close();
    }
}
