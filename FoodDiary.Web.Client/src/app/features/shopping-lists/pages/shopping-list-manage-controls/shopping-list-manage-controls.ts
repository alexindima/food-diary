import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { type FieldTree, FormField } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';
import { FdUiSelectComponent } from 'fd-ui-kit/select/fd-ui-select';

import { buildShoppingListOptions } from '../../lib/shopping-list-options.mapper';
import type { ShoppingListSummary } from '../../models/shopping-list.data';

@Component({
    selector: 'fd-shopping-list-manage-controls',
    imports: [FormField, TranslatePipe, FdUiButtonComponent, FdUiInputComponent, FdUiSelectComponent],
    templateUrl: './shopping-list-manage-controls.html',
    styleUrl: '../shopping-list-page/shopping-list-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShoppingListManageControlsComponent {
    public readonly listSelectField = input.required<FieldTree<string | null>>();
    public readonly listNameField = input.required<FieldTree<string>>();
    public readonly lists = input.required<readonly ShoppingListSummary[]>();
    public readonly isLoading = input.required<boolean>();
    public readonly canDeleteList = input.required<boolean>();
    public readonly canClearList = input.required<boolean>();
    public readonly isMobile = input(false);
    protected readonly listOptions = computed(() => buildShoppingListOptions(this.lists()));
    protected readonly listsCount = computed(() => this.lists().length);

    public readonly createList = output();
    public readonly deleteList = output();
    public readonly clearList = output();
}
