import { ChangeDetectionStrategy, Component, contentChildren, output, TemplateRef, viewChild } from '@angular/core';

import { FdUiMenuItemComponent } from './fd-ui-menu-item.component';

@Component({
    selector: 'fd-ui-menu',
    standalone: true,
    templateUrl: './fd-ui-menu.component.html',
    styleUrls: ['./fd-ui-menu.component.scss'],
    exportAs: 'fdUiMenu',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiMenuComponent {
    private readonly templateRefValue = viewChild.required(TemplateRef<unknown>);
    private readonly menuItems = contentChildren(FdUiMenuItemComponent, { descendants: true });

    public readonly closed = output<void>();

    public get templateRef(): TemplateRef<unknown> {
        return this.templateRefValue();
    }

    public close(): void {
        this.closed.emit();
    }

    public focusFirstItem(): void {
        this.focusItem(this.menuItems().findIndex(item => !item.disabled()));
    }

    public focusLastItem(): void {
        const items = this.menuItems();
        for (let index = items.length - 1; index >= 0; index -= 1) {
            if (!items[index].disabled()) {
                this.focusItem(index);
                return;
            }
        }
    }

    protected onKeydown(event: KeyboardEvent): void {
        const items = this.menuItems().filter(item => !item.disabled());
        if (!items.length) {
            if (event.key === 'Escape') {
                event.preventDefault();
                this.close();
            }
            return;
        }

        const currentIndex = items.findIndex(item => item.isFocused());

        switch (event.key) {
            case 'ArrowDown':
                event.preventDefault();
                items[(currentIndex + 1 + items.length) % items.length].focus();
                break;
            case 'ArrowUp':
                event.preventDefault();
                items[(currentIndex - 1 + items.length) % items.length].focus();
                break;
            case 'Home':
                event.preventDefault();
                items[0].focus();
                break;
            case 'End':
                event.preventDefault();
                items[items.length - 1].focus();
                break;
            case 'Escape':
                event.preventDefault();
                this.close();
                break;
        }
    }

    private focusItem(index: number): void {
        if (index < 0) {
            return;
        }

        queueMicrotask(() => {
            this.menuItems()[index]?.focus();
        });
    }
}
