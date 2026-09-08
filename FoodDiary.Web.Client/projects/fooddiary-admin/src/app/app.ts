import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiSidebarComponent, type FdUiSidebarSection, FdUiTabsComponent } from 'fd-ui-kit';

type AdminPageHeader = {
    title: string;
    subtitle: string;
};

const ADMIN_PAGE_HEADERS: Readonly<Record<string, AdminPageHeader>> = {
    '/': { title: 'ADMIN_NAV.DASHBOARD', subtitle: 'ADMIN_SUBTITLE.DASHBOARD' },
    '/users': { title: 'ADMIN_NAV.ACCOUNTS', subtitle: 'ADMIN_SUBTITLE.ACCOUNTS' },
    '/users/login-activity': { title: 'ADMIN_NAV.LOGINS', subtitle: 'ADMIN_SUBTITLE.LOGINS' },
    '/users/impersonation-sessions': {
        title: 'ADMIN_NAV.SESSIONS',
        subtitle: 'ADMIN_SUBTITLE.SESSIONS',
    },
    '/ai-usage': { title: 'ADMIN_NAV.AI', subtitle: 'ADMIN_SUBTITLE.AI' },
    '/analytics/retention': { title: 'ADMIN_NAV.RETENTION', subtitle: 'ADMIN_RETENTION.SUBTITLE' },
    '/audit': { title: 'ADMIN_NAV.AUDIT', subtitle: 'ADMIN_AUDIT.SUBTITLE' },
    '/bugs': { title: 'ADMIN_NAV.BUGS', subtitle: 'ADMIN_BUGS.SUBTITLE' },
    '/ai-prompts': { title: 'ADMIN_NAV.PROMPTS', subtitle: 'ADMIN_PROMPTS.SUBTITLE' },
    '/acquisition': { title: 'ADMIN_NAV.ACQUISITION', subtitle: 'ADMIN_SUBTITLE.ACQUISITION' },
    '/billing': { title: 'ADMIN_NAV.BILLING', subtitle: 'ADMIN_SUBTITLE.BILLING' },
    '/email-templates': { title: 'ADMIN_NAV.TEMPLATES', subtitle: 'ADMIN_SUBTITLE.TEMPLATES' },
    '/mail-inbox': { title: 'ADMIN_NAV.MAIL', subtitle: 'ADMIN_SUBTITLE.MAIL' },
    '/lessons': { title: 'ADMIN_NAV.LESSONS', subtitle: 'ADMIN_SUBTITLE.LESSONS' },
    '/achievements': { title: 'ADMIN_NAV.ACHIEVEMENTS', subtitle: 'ADMIN_SUBTITLE.ACHIEVEMENTS' },
    '/moderation': { title: 'ADMIN_NAV.MODERATION', subtitle: 'ADMIN_SUBTITLE.MODERATION' },
};

const ADMIN_TOOL_LINKS = [
    { id: 'ai-usage', icon: 'smart_toy', key: 'ADMIN_NAV.AI', route: '/ai-usage' },
    { id: 'retention', icon: 'timeline', key: 'ADMIN_NAV.RETENTION', route: '/analytics/retention' },
    { id: 'audit', icon: 'history', key: 'ADMIN_NAV.AUDIT', route: '/audit' },
    { id: 'bugs', icon: 'bug_report', key: 'ADMIN_NAV.BUGS', route: '/bugs' },
    { id: 'ai-prompts', icon: 'edit_note', key: 'ADMIN_NAV.PROMPTS', route: '/ai-prompts' },
    { id: 'acquisition', icon: 'campaign', key: 'ADMIN_NAV.ACQUISITION', route: '/acquisition' },
    { id: 'billing', icon: 'payments', key: 'ADMIN_NAV.BILLING', route: '/billing' },
    { id: 'email-templates', icon: 'mail', key: 'ADMIN_NAV.TEMPLATES', route: '/email-templates' },
    { id: 'mail-inbox', icon: 'inbox', key: 'ADMIN_OUTGOING.MAIL', route: '/mail-inbox' },
    { id: 'lessons', icon: 'school', key: 'ADMIN_NAV.LESSONS', route: '/lessons' },
    { id: 'achievements', icon: 'emoji_events', key: 'ADMIN_NAV.ACHIEVEMENTS', route: '/achievements' },
    { id: 'moderation', icon: 'gavel', key: 'ADMIN_NAV.MODERATION', route: '/moderation' },
];

@Component({
    selector: 'app-root',
    imports: [RouterOutlet, FdUiSidebarComponent, FdUiTabsComponent, TranslatePipe],
    templateUrl: './app.html',
    styleUrl: './app.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppComponent {
    private readonly router = inject(Router);
    private readonly destroyRef = inject(DestroyRef);
    protected readonly isUsersSectionExpanded = signal(true);
    protected readonly currentUrl = signal(this.router.url);
    protected readonly currentPath = computed(() => this.currentUrl().split('?')[0] ?? '/');
    private readonly translate = inject(TranslateService);
    private readonly language = toSignal(this.translate.onLangChange);
    protected readonly currentSection = computed(
        () =>
            [...this.mobileItems()]
                .sort((a, b) => b.route.length - a.route.length)
                .find(item => this.currentPath() === item.route || (item.route !== '/' && this.currentPath().startsWith(`${item.route}/`)))
                ?.route ?? this.currentPath(),
    );
    protected readonly isMailRoute = computed(() => ['/mail-inbox', '/outgoing-emails'].includes(this.currentPath()));
    protected navigateMail(path: string): void {
        void this.router.navigateByUrl(path);
    }
    protected readonly pageHeader = computed(() => ADMIN_PAGE_HEADERS[this.currentSection()] ?? null);
    protected readonly mobileItems = computed(() => [
        ...this.sidebarSections().flatMap(section =>
            section.items.flatMap(item => ('route' in item ? [{ id: item.id, route: item.route, label: item.label }] : [])),
        ),
        { id: 'outgoing', route: '/outgoing-emails', label: String(this.translate.instant('ADMIN_OUTGOING.OUTGOING')) },
    ]);
    protected readonly sidebarSections = computed<FdUiSidebarSection[]>(() => {
        this.language();
        return [
            {
                id: 'admin-primary',
                items: [
                    {
                        id: 'dashboard',
                        icon: 'dashboard',
                        label: String(this.translate.instant('ADMIN_NAV.DASHBOARD')),
                        route: '/',
                        exact: true,
                    },
                ],
            },
            {
                id: 'users',
                title: String(this.translate.instant('ADMIN_NAV.USERS')),
                collapsible: true,
                expanded: this.isUsersRoute() || this.isUsersSectionExpanded(),
                secondary: true,
                items: [
                    {
                        id: 'users-accounts',
                        icon: 'group',
                        label: String(this.translate.instant('ADMIN_NAV.ACCOUNTS')),
                        route: '/users',
                        exact: true,
                    },
                    {
                        id: 'users-login-activity',
                        icon: 'login',
                        label: String(this.translate.instant('ADMIN_NAV.LOGINS')),
                        route: '/users/login-activity',
                    },
                    {
                        id: 'users-impersonation-sessions',
                        icon: 'admin_panel_settings',
                        label: String(this.translate.instant('ADMIN_NAV.SESSIONS')),
                        route: '/users/impersonation-sessions',
                    },
                ],
            },
            {
                id: 'admin-tools',
                items: ADMIN_TOOL_LINKS.map(item => ({
                    id: item.id,
                    icon: item.icon,
                    route: item.route,
                    label: String(this.translate.instant(item.key)),
                })),
            },
        ];
    });

    public constructor() {
        this.router.events.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(event => {
            if (event instanceof NavigationEnd) {
                this.currentUrl.set(event.urlAfterRedirects);
            }
        });
    }

    protected onSidebarSectionToggled(sectionId: string): void {
        if (sectionId !== 'users') {
            return;
        }

        this.isUsersSectionExpanded.update(isExpanded => !isExpanded);
    }

    protected onMobileRouteChange(event: Event): void {
        const target = event.target;
        if (target === null || !('value' in target) || typeof target.value !== 'string') {
            return;
        }

        const route = target.value;
        if (route.length === 0 || route === this.currentPath()) {
            return;
        }

        void this.router.navigateByUrl(route);
    }

    private isUsersRoute(): boolean {
        return this.currentUrl().startsWith('/users');
    }
}
