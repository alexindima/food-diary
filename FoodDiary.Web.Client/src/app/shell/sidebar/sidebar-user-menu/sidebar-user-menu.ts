import { DOCUMENT, NgOptimizedImage, SlicePipe, UpperCasePipe } from '@angular/common';
import { afterRenderEffect, ChangeDetectionStrategy, Component, effect, ElementRef, inject, input, output, viewChild } from '@angular/core';
import { RouterModule } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiIconComponent } from 'fd-ui-kit';

import type { User } from '../../../shared/models/user.data';
import type { SidebarDirectRouteRequest } from '../sidebar-lib/sidebar.models';
import { focusFirstSidebarInteractiveElement, isSidebarRouterLinkActivation } from '../sidebar-lib/sidebar-view.utils';

@Component({
    selector: 'fd-sidebar-user-menu',
    imports: [NgOptimizedImage, RouterModule, FdUiIconComponent, SlicePipe, TranslatePipe, UpperCasePipe],
    host: { '(focusout)': 'onFocusOut($event)' },
    templateUrl: './sidebar-user-menu.html',
    styleUrl: '../sidebar.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SidebarUserMenuComponent {
    private readonly document = inject(DOCUMENT);
    private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

    public readonly user = input.required<User>();
    public readonly userPlanLabelKey = input.required<string>();
    public readonly isOpen = input.required<boolean>();
    public readonly pendingRoute = input.required<string | null>();
    public readonly isCollapsed = input(false);

    public readonly toggleMenu = output<HTMLElement>();
    public readonly dismissMenu = output();
    public readonly directRouteClick = output<SidebarDirectRouteRequest>();
    public readonly logout = output();

    private readonly userMenuRef = viewChild<ElementRef<HTMLElement>>('userMenu');

    public constructor() {
        effect(onCleanup => {
            if (!this.isOpen()) {
                return;
            }

            const handleOutsidePointerDown = (event: PointerEvent): void => {
                this.dismissIfOutside(event.target);
            };

            this.document.addEventListener('pointerdown', handleOutsidePointerDown, true);
            onCleanup(() => {
                this.document.removeEventListener('pointerdown', handleOutsidePointerDown, true);
            });
        });

        afterRenderEffect(() => {
            const menu = this.userMenuRef();
            if (menu !== undefined) {
                focusFirstSidebarInteractiveElement(menu.nativeElement);
            }
        });
    }

    protected onFocusOut(event: FocusEvent): void {
        if (this.isOpen()) {
            this.dismissIfOutside(event.relatedTarget);
        }
    }

    protected activateProfileRoute(event: MouseEvent): void {
        if (isSidebarRouterLinkActivation(event)) {
            this.directRouteClick.emit({ route: '/profile' });
            this.dismissMenu.emit();
        }
    }

    private dismissIfOutside(target: EventTarget | null): void {
        if (target instanceof Node && !this.host.nativeElement.contains(target)) {
            this.dismissMenu.emit();
        }
    }
}
