import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { FdUiSidebarComponent, type FdUiSidebarSection } from 'fd-ui-kit';

type AdminPageHeader = {
    title: string;
    subtitle: string;
};

const ADMIN_PAGE_HEADERS: Readonly<Record<string, AdminPageHeader>> = {
    '/': { title: 'Dashboard', subtitle: 'Product health and operational activity.' },
    '/users': { title: 'Accounts', subtitle: 'Manage user access, status and profile data.' },
    '/users/login-activity': { title: 'Login activity', subtitle: 'Review successful authentication events and clients.' },
    '/users/impersonation-sessions': {
        title: 'Impersonation sessions',
        subtitle: 'Audit administrative access performed on behalf of users.',
    },
    '/ai-usage': { title: 'AI logs', subtitle: 'Monitor token usage and AI-assisted operations.' },
    '/acquisition': { title: 'Acquisition', subtitle: 'Track campaign attribution and conversion activity.' },
    '/billing': { title: 'Billing', subtitle: 'Review subscriptions, payments and webhook events.' },
    '/email-templates': { title: 'Email templates', subtitle: 'Create and maintain transactional email content.' },
    '/mail-inbox': { title: 'Mail inbox', subtitle: 'Inspect inbound messages and DMARC reports.' },
    '/lessons': { title: 'Lessons', subtitle: 'Publish and maintain nutrition academy content.' },
    '/moderation': { title: 'Moderation', subtitle: 'Review reports and resolve content issues.' },
};

@Component({
    selector: 'app-root',
    imports: [RouterOutlet, FdUiSidebarComponent],
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
    protected readonly pageHeader = computed(() => ADMIN_PAGE_HEADERS[this.currentPath()] ?? null);
    protected readonly sidebarSections = computed<FdUiSidebarSection[]>(() => [
        {
            id: 'admin-primary',
            items: [{ id: 'dashboard', icon: 'dashboard', label: 'Dashboard', route: '/', exact: true }],
        },
        {
            id: 'users',
            title: 'Users',
            collapsible: true,
            expanded: this.isUsersRoute() || this.isUsersSectionExpanded(),
            secondary: true,
            items: [
                { id: 'users-accounts', icon: 'group', label: 'Accounts', route: '/users', exact: true },
                { id: 'users-login-activity', icon: 'login', label: 'Login activity', route: '/users/login-activity' },
                {
                    id: 'users-impersonation-sessions',
                    icon: 'admin_panel_settings',
                    label: 'Impersonation sessions',
                    route: '/users/impersonation-sessions',
                },
            ],
        },
        {
            id: 'admin-tools',
            items: [
                { id: 'ai-usage', icon: 'smart_toy', label: 'AI Logs', route: '/ai-usage' },
                { id: 'acquisition', icon: 'campaign', label: 'Acquisition', route: '/acquisition' },
                { id: 'billing', icon: 'payments', label: 'Billing', route: '/billing' },
                { id: 'email-templates', icon: 'mail', label: 'Email templates', route: '/email-templates' },
                { id: 'mail-inbox', icon: 'inbox', label: 'Mail inbox', route: '/mail-inbox' },
                { id: 'lessons', icon: 'school', label: 'Lessons', route: '/lessons' },
                { id: 'moderation', icon: 'gavel', label: 'Moderation', route: '/moderation' },
            ],
        },
    ]);

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
