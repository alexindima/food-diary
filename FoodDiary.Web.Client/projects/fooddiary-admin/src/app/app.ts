import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { FdUiSidebarComponent, type FdUiSidebarSection } from 'fd-ui-kit';

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

    private isUsersRoute(): boolean {
        return this.currentUrl().startsWith('/users');
    }
}
