import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { type FormControl, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiSelectComponent, type FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select.component';

@Component({
    selector: 'fd-shopping-list-manage-controls',
    imports: [ReactiveFormsModule, TranslatePipe, FdUiButtonComponent, FdUiInputComponent, FdUiSelectComponent],
    templateUrl: './shopping-list-manage-controls.component.html',
    styleUrl: './shopping-list-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShoppingListManageControlsComponent {
    public readonly listSelectControl = input.required<FormControl<string | null>>();
    public readonly listNameControl = input.required<FormControl<string>>();
    public readonly listOptions = input.required<Array<FdUiSelectOption<string>>>();
    public readonly listsCount = input.required<number>();
    public readonly isLoading = input.required<boolean>();
    public readonly canDeleteList = input.required<boolean>();
    public readonly canClearList = input.required<boolean>();
    public readonly isMobile = input(false);

    public readonly createList = output();
    public readonly deleteList = output();
    public readonly clearList = output();
}
