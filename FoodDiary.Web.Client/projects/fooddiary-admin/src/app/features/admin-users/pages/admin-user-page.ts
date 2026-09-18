import { DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiCardComponent, FdUiDialogService } from 'fd-ui-kit';
import { catchError, combineLatest, forkJoin, type Observable, of, startWith, Subject, switchMap } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { AdminLoadErrorComponent } from '../../../shared/feedback/admin-load-error';
import { AdminUserDetailsBodyComponent } from '../components/admin-user-details-body';
import { AdminUserEditDialogComponent } from '../dialogs/admin-user-edit-dialog';
import { AdminUserImpersonationDialogComponent } from '../dialogs/admin-user-impersonation-dialog';
import { AdminUserSetPasswordDialogComponent } from '../dialogs/admin-user-set-password-dialog';
import { buildAdminUserSections } from '../lib/admin-user-sections';
import { AdminUsersFacade } from '../lib/admin-users.facade';
import type {
    AdminImpersonationStart,
    AdminUser,
    AdminUserLoginEvent,
    AdminUserRoleAuditEvent,
    PagedResponse,
} from '../models/admin-user.models';

const ACTIVITY_PREVIEW_LIMIT = 3;

@Component({
    selector: 'fd-admin-user-page',
    imports: [AdminUserDetailsBodyComponent, RouterLink, TranslatePipe, FdUiButtonComponent, FdUiCardComponent, AdminLoadErrorComponent],
    templateUrl: './admin-user-page.html',
    styleUrl: './admin-user-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: { class: 'fd-grid fd-gap-md' },
})
export class AdminUserPageComponent {
    private readonly route = inject(ActivatedRoute);
    private readonly users = inject(AdminUsersFacade);
    private readonly dialogs = inject(FdUiDialogService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly document = inject(DOCUMENT);
    private readonly refresh = new Subject<void>();
    protected readonly user = signal<AdminUser | null>(null);
    protected readonly failed = signal(false);
    private readonly translate = inject(TranslateService);
    protected readonly activityLoading = signal(true);
    protected readonly activityFailed = signal(false);
    protected readonly loginEvents = signal<AdminUserLoginEvent[]>([]);
    protected readonly roleAuditEvents = signal<AdminUserRoleAuditEvent[]>([]);
    private readonly passwordChangeLabel = toSignal(this.translate.stream('ADMIN_USER.PASSWORD_CHANGE_REQUIRED'), { initialValue: '' });
    protected readonly sections = computed(() => {
        const user = this.user();
        return user === null ? [] : buildAdminUserSections(user, String(this.passwordChangeLabel()));
    });
    protected readonly initials = computed(() => {
        const user = this.user();
        return user === null ? '?' : this.buildInitials(user);
    });
    protected readonly canImpersonate = computed(() => {
        const user = this.user();
        return user !== null && (user.deletedAt === null || user.deletedAt === undefined) && !user.roles.includes('Admin');
    });

    public constructor() {
        combineLatest([this.route.paramMap, this.refresh.pipe(startWith(undefined))])
            .pipe(
                switchMap(([params]) => {
                    this.user.set(null);
                    this.failed.set(false);
                    this.activityLoading.set(true);
                    this.activityFailed.set(false);
                    this.loginEvents.set([]);
                    this.roleAuditEvents.set([]);
                    return this.users.getUser(params.get('id') ?? '').pipe(
                        catchError(() => {
                            this.failed.set(true);
                            return of(null);
                        }),
                        switchMap(user => {
                            this.user.set(user);
                            return user === null ? of(null) : this.loadActivity(user.id);
                        }),
                    );
                }),
                takeUntilDestroyed(),
            )
            .subscribe(activity => {
                this.loginEvents.set(activity?.loginEvents.items ?? []);
                this.roleAuditEvents.set(activity?.roleAuditEvents ?? []);
                this.activityLoading.set(false);
            });
    }

    protected reload(): void {
        this.refresh.next();
    }

    protected edit(user: AdminUser): void {
        this.dialogs
            .open(AdminUserEditDialogComponent, { size: 'sm', data: user })
            .afterClosed()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(updated => {
                if (updated === true) {
                    this.reload();
                }
            });
    }

    protected setPassword(user: AdminUser): void {
        this.dialogs
            .open<AdminUserSetPasswordDialogComponent, AdminUser, boolean>(AdminUserSetPasswordDialogComponent, { size: 'sm', data: user })
            .afterClosed()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(updated => {
                if (updated === true) {
                    this.reload();
                }
            });
    }

    private buildInitials(user: AdminUser): string {
        const initials = `${user.firstName?.trim()[0] ?? ''}${user.lastName?.trim()[0] ?? ''}`;
        return (initials.length > 0 ? initials : (user.email?.trim().at(0) ?? '?')).toUpperCase();
    }

    private loadActivity(
        userId: string,
    ): Observable<{ loginEvents: PagedResponse<AdminUserLoginEvent>; roleAuditEvents: AdminUserRoleAuditEvent[] } | null> {
        return forkJoin({
            loginEvents: this.users.getLoginEvents(1, ACTIVITY_PREVIEW_LIMIT, null, { userId }),
            roleAuditEvents: this.users.getUserRoleAudit(userId),
        }).pipe(
            catchError(() => {
                this.activityFailed.set(true);
                return of(null);
            }),
        );
    }

    protected impersonate(user: AdminUser): void {
        if (!this.canImpersonate()) {
            return;
        }
        this.dialogs
            .open<AdminUserImpersonationDialogComponent, AdminUser, AdminImpersonationStart | null>(AdminUserImpersonationDialogComponent, {
                size: 'sm',
                data: user,
            })
            .afterClosed()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(result => {
                if (result === null || result === undefined) {
                    return;
                }
                const url = new URL('/dashboard', environment.mainAppUrl);
                url.searchParams.set('impersonationCode', result.code);
                this.document.defaultView?.open(url.toString(), '_blank', 'noopener,noreferrer');
            });
    }
}
