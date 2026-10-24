import {
    afterNextRender,
    ChangeDetectionStrategy,
    Component,
    computed,
    DestroyRef,
    type ElementRef,
    inject,
    input,
    output,
    signal,
    viewChild,
} from '@angular/core';
import { RouterModule } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiHintDirective, FdUiIconComponent, FdUiModalDirective } from 'fd-ui-kit';

import type { MobileSheetId, SidebarActionId, SidebarDirectRouteRequest, SidebarRouteItem } from '../sidebar-lib/sidebar.models';
import { BODY_TRACKING_ITEMS, FOOD_TRACKING_ITEMS, MOBILE_REPORT_ITEMS } from '../sidebar-lib/sidebar-navigation.config';
import { getSidebarMobileSheetLabelKey, getSidebarMobileSheetRouteItems } from '../sidebar-lib/sidebar-view.utils';
import { SidebarRouteLinksComponent } from '../sidebar-route-links/sidebar-route-links';

@Component({
    selector: 'fd-sidebar-mobile',
    imports: [FdUiModalDirective, RouterModule, FdUiHintDirective, FdUiIconComponent, SidebarRouteLinksComponent, TranslatePipe],
    templateUrl: './sidebar-mobile.html',
    styleUrl: '../sidebar.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SidebarMobileComponent {
    private readonly destroyRef = inject(DestroyRef);

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

    private readonly mobileNav = viewChild<ElementRef<HTMLElement>>('mobileNav');
    protected readonly sheetBottom = signal(0);
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
        afterNextRender(() => {
            const nav = this.mobileNav()?.nativeElement;
            if (nav === undefined) {
                return;
            }
            const update = (): void => {
                this.sheetBottom.set(nav.getBoundingClientRect().height);
            };
            update();
            const Observer = nav.ownerDocument.defaultView?.ResizeObserver;
            if (Observer !== undefined) {
                const observer = new Observer(update);
                observer.observe(nav);
                this.destroyRef.onDestroy(() => {
                    observer.disconnect();
                });
            }
        });
    }
}
