import { ChangeDetectionStrategy, Component, computed, effect, type ElementRef, input, output, viewChild } from '@angular/core';
import { RouterModule } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiIconComponent } from 'fd-ui-kit';

import type { MobileSheetId, SidebarActionId, SidebarDirectRouteRequest, SidebarRouteItem } from '../sidebar-lib/sidebar.models';
import { BODY_TRACKING_ITEMS, FOOD_TRACKING_ITEMS, MOBILE_REPORT_ITEMS } from '../sidebar-lib/sidebar-navigation.config';
import {
    focusFirstSidebarInteractiveElement,
    getSidebarMobileSheetLabelKey,
    getSidebarMobileSheetRouteItems,
} from '../sidebar-lib/sidebar-view.utils';
import { SidebarMobileSheetComponent } from '../sidebar-mobile-sheet/sidebar-mobile-sheet.component';

@Component({
    selector: 'fd-sidebar-mobile',
    imports: [RouterModule, FdUiIconComponent, SidebarMobileSheetComponent, TranslatePipe],
    templateUrl: './sidebar-mobile.component.html',
    styleUrl: '../sidebar.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SidebarMobileComponent {
    public readonly isProgressVisible = input.required<boolean>();
    public readonly dailyConsumedKcalRounded = input.required<number>();
    public readonly dailyGoalKcalRounded = input.required<number>();
    public readonly dailyProgressPercent = input.required<number>();
    public readonly pendingRoute = input.required<string | null>();
    public readonly unreadNotificationCount = input.required<number>();
    public readonly mobileSheet = input.required<MobileSheetId>();
    public readonly isAdmin = input.required<boolean>();

    public readonly directRouteClick = output<SidebarDirectRouteRequest>();
    public readonly closeMenus = output();
    public readonly foodToggle = output<HTMLElement>();
    public readonly bodyToggle = output<HTMLElement>();
    public readonly reportsToggle = output<HTMLElement>();
    public readonly userToggle = output<HTMLElement>();
    public readonly mobileAction = output<SidebarActionId>();
    public readonly routeSelected = output<SidebarRouteItem>();

    private readonly mobileSheetRef = viewChild<ElementRef<HTMLElement>>('mobileSheetPanel');
    protected readonly isFoodOpen = computed(() => this.mobileSheet() === 'food');
    protected readonly isBodyOpen = computed(() => this.mobileSheet() === 'body');
    protected readonly isReportsOpen = computed(() => this.mobileSheet() === 'reports');
    protected readonly isUserOpen = computed(() => this.mobileSheet() === 'user');
    protected readonly isSheetOpen = computed(() => this.mobileSheet() !== null);
    protected readonly activeSheetLabelKey = computed(() => getSidebarMobileSheetLabelKey(this.mobileSheet()));
    protected readonly activeRouteItems = computed<SidebarRouteItem[]>(() =>
        getSidebarMobileSheetRouteItems(this.mobileSheet(), FOOD_TRACKING_ITEMS, BODY_TRACKING_ITEMS, MOBILE_REPORT_ITEMS),
    );

    public constructor() {
        effect(() => {
            if (!this.isSheetOpen()) {
                return;
            }

            queueMicrotask(() => {
                focusFirstSidebarInteractiveElement(this.mobileSheetRef()?.nativeElement);
            });
        });
    }
}
