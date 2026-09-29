import { NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { type FieldTree, FormField, FormRoot } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCheckboxComponent } from 'fd-ui-kit/checkbox/fd-ui-checkbox';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';
import { FdUiMenuComponent } from 'fd-ui-kit/menu/fd-ui-menu';
import { FdUiMenuItemComponent } from 'fd-ui-kit/menu/fd-ui-menu-item';
import { FdUiMenuTriggerDirective } from 'fd-ui-kit/menu/fd-ui-menu-trigger.directive';
import { FdUiSelectComponent } from 'fd-ui-kit/select/fd-ui-select';

import type { ShoppingListItemFormModel } from '../../lib/shopping-list-form.types';
import { buildShoppingListItemViewModels, buildShoppingListUnitOptions } from '../../lib/shopping-list-item.mapper';
import type { ShoppingListItem } from '../../models/shopping-list.data';

const CHECKED_SETTLE_MS = 300;

@Component({
    selector: 'fd-shopping-list-items-panel',
    imports: [
        NgTemplateOutlet,
        FdUiMenuComponent,
        FdUiMenuItemComponent,
        FdUiMenuTriggerDirective,
        FormField,
        FormRoot,
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
    protected readonly purchasedOpen = signal(true);
    private readonly settlingIds = signal<ReadonlySet<string>>(new Set());
    private readonly settleTimers = new Set<ReturnType<typeof setTimeout>>();
    protected readonly pendingItems = computed(() =>
        this.itemViewModels().filter(item => !item.isChecked || this.settlingIds().has(item.id)),
    );
    protected readonly purchasedItems = computed(() =>
        this.itemViewModels().filter(item => item.isChecked && !this.settlingIds().has(item.id)),
    );
    protected readonly areDetailsVisible = signal(false);

    public readonly itemForm = input.required<FieldTree<ShoppingListItemFormModel>>();
    public readonly items = input.required<readonly ShoppingListItem[]>();
    protected readonly unitOptions = computed(() => {
        this.activeLang();
        return buildShoppingListUnitOptions(key => this.translateService.instant(key));
    });
    protected readonly itemViewModels = computed(() => {
        this.activeLang();
        return buildShoppingListItemViewModels(this.items(), key => this.translateService.instant(key));
    });
    protected readonly isItemFormInvalid = computed(() => this.itemForm()().invalid());

    public readonly itemAdd = output();
    public readonly itemRemove = output<string>();
    public readonly itemEdit = output<string>();
    public readonly itemCheckedChange = output<{ itemId: string; checked: boolean }>();

    public constructor() {
        this.destroyRef.onDestroy(() => {
            this.settleTimers.forEach(timer => {
                clearTimeout(timer);
            });
        });
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(event => {
            this.activeLang.set(event.lang);
        });
    }

    protected toggleChecked(itemId: string, checked: boolean): void {
        if (checked) {
            this.settlingIds.update(ids => new Set([...ids, itemId]));
            const timer = setTimeout(() => {
                this.settlingIds.update(ids => new Set([...ids].filter(id => id !== itemId)));
                this.settleTimers.delete(timer);
            }, CHECKED_SETTLE_MS);
            this.settleTimers.add(timer);
        }
        this.itemCheckedChange.emit({ itemId, checked });
    }

    protected toggleDetails(): void {
        this.areDetailsVisible.update(value => !value);
    }

    protected onItemFormSubmit(event: SubmitEvent): void {
        event.preventDefault();
        this.itemAdd.emit();
    }
}
