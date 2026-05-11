import { ChangeDetectionStrategy, Component, effect, type ElementRef, input, output, viewChild } from '@angular/core';
import { RouterModule } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiIconComponent } from 'fd-ui-kit';

import type { MobileSheetId, SidebarActionId, SidebarDirectRouteRequest, SidebarRouteItem } from './sidebar.models';
import { SidebarMobileSheetComponent } from './sidebar-mobile-sheet.component';

@Component({
    selector: 'fd-sidebar-mobile',
    imports: [RouterModule, FdUiIconComponent, SidebarMobileSheetComponent, TranslatePipe],
    templateUrl: './sidebar-mobile.component.html',
    styleUrl: './sidebar.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SidebarMobileComponent {
    public readonly isProgressVisible = input.required<boolean>();
    public readonly dailyConsumedKcalRounded = input.required<number>();
    public readonly dailyGoalKcalRounded = input.required<number>();
    public readonly dailyProgressPercent = input.required<number>();
    public readonly pendingRoute = input.required<string | null>();
    public readonly isFoodOpen = input.required<boolean>();
    public readonly isBodyOpen = input.required<boolean>();
    public readonly isReportsOpen = input.required<boolean>();
    public readonly isUserOpen = input.required<boolean>();
    public readonly unreadNotificationCount = input.required<number>();
    public readonly isSheetOpen = input.required<boolean>();
    public readonly activeSheetLabelKey = input.required<string>();
    public readonly mobileSheet = input.required<MobileSheetId>();
    public readonly isAdmin = input.required<boolean>();
    public readonly activeRouteItems = input.required<SidebarRouteItem[]>();

    public readonly directRouteClick = output<SidebarDirectRouteRequest>();
    public readonly closeMenus = output<void>();
    public readonly foodToggle = output<HTMLElement>();
    public readonly bodyToggle = output<HTMLElement>();
    public readonly reportsToggle = output<HTMLElement>();
    public readonly userToggle = output<HTMLElement>();
    public readonly mobileAction = output<SidebarActionId>();
    public readonly routeSelected = output<SidebarRouteItem>();

    private readonly mobileSheetRef = viewChild<ElementRef<HTMLElement>>('mobileSheetPanel');

    private readonly mobileSheetFocusSync = effect(() => {
        if (!this.isSheetOpen()) {
            return;
        }

        queueMicrotask(() => {
            this.focusFirstInteractive(this.mobileSheetRef()?.nativeElement);
        });
    });

    private focusFirstInteractive(container?: HTMLElement | null): void {
        if (container === null || container === undefined) {
            return;
        }

        const firstInteractive = container.querySelector<HTMLElement>('button:not([disabled]), a[href], [tabindex]:not([tabindex="-1"])');
        firstInteractive?.focus();
    }
}
