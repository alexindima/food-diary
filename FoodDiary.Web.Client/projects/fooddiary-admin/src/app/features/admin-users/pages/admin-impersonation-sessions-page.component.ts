import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { type AdminImpersonationSession, AdminUsersService } from '../api/admin-users.service';
import { AdminSessionsSectionComponent } from './admin-sessions-section.component';

const ADMIN_IMPERSONATION_SESSIONS_PAGE_SIZE = 20;

@Component({
    selector: 'fd-admin-impersonation-sessions-page',
    imports: [AdminSessionsSectionComponent],
    templateUrl: './admin-impersonation-sessions-page.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminImpersonationSessionsPageComponent {
    private readonly usersService = inject(AdminUsersService);
    private readonly destroyRef = inject(DestroyRef);
    protected readonly pageSize = ADMIN_IMPERSONATION_SESSIONS_PAGE_SIZE;

    protected readonly sessions = signal<AdminImpersonationSession[]>([]);
    protected readonly sessionsPage = signal(1);
    protected readonly sessionsTotalPages = signal(1);
    protected readonly sessionsTotalItems = signal(0);
    protected readonly sessionsSearch = signal('');
    protected readonly isSessionsLoading = signal(false);

    public constructor() {
        this.loadSessions();
    }

    protected onSessionsSearchChange(value: string): void {
        this.sessionsSearch.set(value);
        this.sessionsPage.set(1);
        this.loadSessions();
    }

    protected goToSessionsPage(page: number): void {
        if (page < 1 || page > this.sessionsTotalPages()) {
            return;
        }

        this.sessionsPage.set(page);
        this.loadSessions();
    }

    private loadSessions(): void {
        this.isSessionsLoading.set(true);
        this.usersService
            .getImpersonationSessions(
                this.sessionsPage(),
                ADMIN_IMPERSONATION_SESSIONS_PAGE_SIZE,
                this.resolveSearchQuery(this.sessionsSearch()),
            )
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: response => {
                    this.sessions.set(response.items);
                    this.sessionsTotalPages.set(response.totalPages);
                    this.sessionsTotalItems.set(response.totalItems);
                    this.isSessionsLoading.set(false);
                },
                error: () => {
                    this.sessions.set([]);
                    this.sessionsTotalPages.set(1);
                    this.sessionsTotalItems.set(0);
                    this.isSessionsLoading.set(false);
                },
            });
    }

    private resolveSearchQuery(value: string): string | null {
        const trimmed = value.trim();
        return trimmed.length > 0 ? trimmed : null;
    }
}
