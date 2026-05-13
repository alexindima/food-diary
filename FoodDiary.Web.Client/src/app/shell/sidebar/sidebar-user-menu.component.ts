import { NgOptimizedImage, SlicePipe, UpperCasePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, effect, type ElementRef, input, output, viewChild } from '@angular/core';
import { RouterModule } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiIconComponent } from 'fd-ui-kit';

import type { User } from '../../shared/models/user.data';
import type { SidebarDirectRouteRequest } from './sidebar.models';

@Component({
    selector: 'fd-sidebar-user-menu',
    imports: [NgOptimizedImage, RouterModule, FdUiButtonComponent, FdUiIconComponent, SlicePipe, TranslatePipe, UpperCasePipe],
    templateUrl: './sidebar-user-menu.component.html',
    styleUrl: './sidebar.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SidebarUserMenuComponent {
    public readonly user = input.required<User>();
    public readonly userPlanLabelKey = input.required<string>();
    public readonly isOpen = input.required<boolean>();
    public readonly pendingRoute = input.required<string | null>();

    public readonly toggleMenu = output<HTMLElement>();
    public readonly directRouteClick = output<SidebarDirectRouteRequest>();
    public readonly logout = output();

    private readonly userMenuRef = viewChild<ElementRef<HTMLElement>>('userMenu');

    public constructor() {
        effect(() => {
            if (!this.isOpen()) {
                return;
            }

            queueMicrotask(() => {
                this.focusFirstInteractive(this.userMenuRef()?.nativeElement);
            });
        });
    }

    private focusFirstInteractive(container?: HTMLElement | null): void {
        if (container === null || container === undefined) {
            return;
        }

        const firstInteractive = container.querySelector<HTMLElement>('button:not([disabled]), a[href], [tabindex]:not([tabindex="-1"])');
        firstInteractive?.focus();
    }
}
