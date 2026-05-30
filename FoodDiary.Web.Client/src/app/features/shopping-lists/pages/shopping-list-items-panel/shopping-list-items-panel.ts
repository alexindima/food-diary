import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { type FormGroup, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCheckboxComponent } from 'fd-ui-kit/checkbox/fd-ui-checkbox';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';
import { FdUiSelectComponent } from 'fd-ui-kit/select/fd-ui-select';

import type { ShoppingListItemFormGroup } from '../../lib/shopping-list-form.types';
import { buildShoppingListItemViewModels, buildShoppingListUnitOptions } from '../../lib/shopping-list-item.mapper';
import type { ShoppingListItem } from '../../models/shopping-list.data';

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
    templateUrl: './shopping-list-items-panel.html',
    styleUrl: '../shopping-list-page/shopping-list-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShoppingListItemsPanelComponent {
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly activeLang = signal(this.translateService.getCurrentLang());

    public readonly itemForm = input.required<FormGroup<ShoppingListItemFormGroup>>();
    public readonly items = input.required<readonly ShoppingListItem[]>();
    protected readonly unitOptions = computed(() => {
        this.activeLang();
        return buildShoppingListUnitOptions(key => this.translateService.instant(key));
    });
    protected readonly itemViewModels = computed(() => {
        this.activeLang();
        return buildShoppingListItemViewModels(this.items(), key => this.translateService.instant(key));
    });

    public readonly itemAdd = output();
    public readonly itemRemove = output<string>();
    public readonly itemCheckedChange = output<{ itemId: string; checked: boolean }>();

    public constructor() {
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(event => {
            this.activeLang.set(event.lang);
        });
    }
}
