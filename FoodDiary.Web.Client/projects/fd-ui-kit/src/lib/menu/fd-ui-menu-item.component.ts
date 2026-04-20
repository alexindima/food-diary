import { ChangeDetectionStrategy, Component, ElementRef, inject, input, output } from '@angular/core';
import { RouterModule } from '@angular/router';
import { FdUiMenuComponent } from './fd-ui-menu.component';

@Component({
    selector: 'fd-ui-menu-item',
    standalone: true,
    imports: [RouterModule],
    template: `
        <button
            class="fd-ui-menu__item"
            [class.fd-ui-menu__item--disabled]="disabled()"
            [type]="type()"
            [routerLink]="routerLink()"
            [queryParams]="queryParams()"
            [fragment]="fragment()"
            [disabled]="disabled()"
            [attr.role]="'menuitem'"
            tabindex="-1"
            (click)="onClick($event)"
        >
            <ng-content />
        </button>
    `,
    styleUrls: ['./fd-ui-menu.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiMenuItemComponent {
    private readonly host = inject(ElementRef<HTMLButtonElement>);
    private readonly parentMenu = inject(FdUiMenuComponent, { optional: true });

    public readonly type = input<'button' | 'submit' | 'reset'>('button');
    public readonly routerLink = input<string | any[] | null>();
    public readonly queryParams = input<Record<string, unknown>>();
    public readonly fragment = input<string>();
    public readonly disabled = input(false);
    public readonly itemClick = output<Event>();

    public focus(): void {
        this.host.nativeElement.focus();
    }

    public isFocused(): boolean {
        return document.activeElement === this.host.nativeElement;
    }

    public onClick(event: Event): void {
        this.itemClick.emit(event);

        if (!event.defaultPrevented && !this.disabled()) {
            this.parentMenu?.close();
        }
    }
}
