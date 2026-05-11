import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCheckboxComponent } from 'fd-ui-kit/checkbox/fd-ui-checkbox.component';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiSelectComponent, type FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select.component';

import type { MeasurementUnit } from '../../products/models/product.data';
import type { ShoppingListItemFormGroup, ShoppingListItemViewModel } from './shopping-list-page.component';

@Component({
    selector: 'fd-shopping-list-items-panel',
    imports: [
        ReactiveFormsModule,
        FormsModule,
        TranslatePipe,
        FdUiHintDirective,
        FdUiButtonComponent,
        FdUiIconComponent,
        FdUiInputComponent,
        FdUiSelectComponent,
        FdUiCheckboxComponent,
    ],
    templateUrl: './shopping-list-items-panel.component.html',
    styleUrl: './shopping-list-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShoppingListItemsPanelComponent {
    public readonly itemForm = input.required<ShoppingListItemFormGroup>();
    public readonly unitOptions = input.required<readonly FdUiSelectOption<MeasurementUnit>[]>();
    public readonly items = input.required<readonly ShoppingListItemViewModel[]>();

    public readonly itemAdd = output<void>();
    public readonly itemRemove = output<string>();
    public readonly itemCheckedChange = output<{ item: ShoppingListItemViewModel; checked: boolean }>();
}
