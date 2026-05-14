import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { type FormControl, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiSelectComponent } from 'fd-ui-kit/select/fd-ui-select.component';

import { buildShoppingListOptions } from '../../lib/shopping-list-options.mapper';
import type { ShoppingListSummary } from '../../models/shopping-list.data';

@Component({
    selector: 'fd-shopping-list-manage-controls',
    imports: [ReactiveFormsModule, TranslatePipe, FdUiButtonComponent, FdUiInputComponent, FdUiSelectComponent],
    templateUrl: './shopping-list-manage-controls.component.html',
    styleUrl: '../shopping-list-page/shopping-list-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShoppingListManageControlsComponent {
    public readonly listSelectControl = input.required<FormControl<string | null>>();
    public readonly listNameControl = input.required<FormControl<string>>();
    public readonly lists = input.required<readonly ShoppingListSummary[]>();
    public readonly isLoading = input.required<boolean>();
    public readonly canDeleteList = input.required<boolean>();
    public readonly canClearList = input.required<boolean>();
    public readonly isMobile = input(false);
    public readonly listOptions = computed(() => buildShoppingListOptions(this.lists()));
    public readonly listsCount = computed(() => this.lists().length);

    public readonly createList = output();
    public readonly deleteList = output();
    public readonly clearList = output();
}
