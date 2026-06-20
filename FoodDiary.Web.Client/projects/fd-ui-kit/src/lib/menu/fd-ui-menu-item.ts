import { DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, ElementRef, inject, input, output } from '@angular/core';
import { RouterModule } from '@angular/router';

import { FD_UI_MENU, FD_UI_MENU_ITEM } from './fd-ui-menu.tokens';

@Component({
    selector: 'fd-ui-menu-item',
    imports: [RouterModule],
    templateUrl: './fd-ui-menu-item.html',
    styleUrls: ['./fd-ui-menu.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        '[attr.title]': 'effectiveDisabledReason()',
    },
    providers: [
        {
            provide: FD_UI_MENU_ITEM,
            useExisting: FdUiMenuItemComponent,
        },
    ],
})
export class FdUiMenuItemComponent {
    private readonly host = inject<ElementRef<HTMLButtonElement>>(ElementRef);
    private readonly parentMenu = inject(FD_UI_MENU, { optional: true });
    private readonly document = inject(DOCUMENT);

    public readonly type = input<'button' | 'submit' | 'reset'>('button');
    public readonly routerLink = input<string | unknown[] | null>();
    public readonly queryParams = input<Record<string, unknown>>();
    public readonly fragment = input<string>();
    public readonly disabled = input(false);
    public readonly disabledReason = input<string | null>(null);
    public readonly itemClick = output<Event>();

    protected readonly effectiveDisabledReason = computed(() => (this.disabled() ? this.disabledReason() : null));

    protected focus(): void {
        this.host.nativeElement.focus();
    }

    protected isFocused(): boolean {
        return this.document.activeElement === this.host.nativeElement;
    }

    protected selectMenuItem(event: Event): void {
        this.itemClick.emit(event);

        if (!event.defaultPrevented && !this.disabled()) {
            this.parentMenu?.close();
        }
    }
}
