import { DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, linkedSignal, signal } from '@angular/core';
import { rxResource, takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiCardComponent, FdUiDialogService } from 'fd-ui-kit';
import { forkJoin, map } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { AdminLoadErrorComponent } from '../../../shared/feedback/admin-load-error';
import { adminId } from '../../../shared/models/semantics/admin-meaning';
import { AdminUserDetailsBodyComponent } from '../components/admin-user-details-body';
import { AdminUserPremiumGrantComponent } from '../components/admin-user-premium-grant';
import { AdminUserEditDialogComponent } from '../dialogs/admin-user-edit-dialog';
import { AdminUserImpersonationDialogComponent } from '../dialogs/admin-user-impersonation-dialog';
import { AdminUserNotificationDialogComponent } from '../dialogs/admin-user-notification-dialog';
import { AdminUserSetPasswordDialogComponent } from '../dialogs/admin-user-set-password-dialog';
import { resolveAdminAvatarUrl } from '../lib/admin-avatar-url';
import { buildAdminUserSections } from '../lib/admin-user-sections';
import { AdminUsersFacade } from '../lib/admin-users.facade';
import type { AdminImpersonationStart, AdminUser } from '../models/admin-user.models';

const ACTIVITY_PREVIEW_LIMIT = 3;

@Component({
    selector: 'fd-admin-user-page',
    imports: [
        AdminUserDetailsBodyComponent,
        AdminUserPremiumGrantComponent,
        RouterLink,
        TranslatePipe,
        FdUiButtonComponent,
        FdUiCardComponent,
        AdminLoadErrorComponent,
    ],
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
    // The account belongs to the path segment. Matrix parameters may contain a separate id.
    private readonly accountId = toSignal(this.route.url.pipe(map(([segment]) => adminId<'user'>(segment.path))), { requireSync: true });
    private readonly loadVersion = signal(0);
    private readonly userRequest = computed(() => ({ id: this.accountId(), version: this.loadVersion() }));
    private readonly userResource = rxResource({ params: this.userRequest, stream: ({ params }) => this.users.getUser(params.id) });
    protected readonly user = computed(() => (this.userResource.hasValue() ? this.userResource.value() : null));
    protected readonly failed = computed(() => this.userResource.error() !== undefined);
    private readonly activityResource = rxResource({
        params: () => {
            const user = this.user();
            return user === null ? undefined : { id: user.id, owner: this.userRequest() };
        },
        stream: ({ params }) =>
            forkJoin({
                loginEvents: this.users.getLoginEvents(1, ACTIVITY_PREVIEW_LIMIT, null, { userId: params.id }),
                roleAuditEvents: this.users.getUserRoleAudit(params.id),
            }),
    });
    private readonly activity = computed(() => (this.activityResource.hasValue() ? this.activityResource.value() : null));
    private readonly translate = inject(TranslateService);
    protected readonly activityLoading = computed(() => this.userResource.isLoading() || this.activityResource.isLoading());
    protected readonly activityFailed = computed(() => this.activityResource.error() !== undefined);
    protected readonly loginEvents = computed(() => this.activity()?.loginEvents.items ?? []);
    protected readonly roleAuditEvents = computed(() => this.activity()?.roleAuditEvents ?? []);
    private readonly passwordChangeLabel = toSignal(this.translate.stream('ADMIN_USER.PASSWORD_CHANGE_REQUIRED'), { initialValue: '' });
    private readonly lastActiveLabel = toSignal(this.translate.stream('ADMIN_USER.LAST_ACTIVE'), { initialValue: '' });
    private readonly unknownActivityLabel = toSignal(this.translate.stream('ADMIN_USER.ACTIVITY_UNKNOWN'), { initialValue: '' });
    private readonly avatarLoadFailure = linkedSignal({
        source: this.userRequest,
        computation: (): { userId: AdminUser['id']; url: string } | null => null,
    });
    protected readonly details = computed(() => {
        const account = this.user();
        if (account === null) {
            return null;
        }
        const url = resolveAdminAvatarUrl(account.profileImage, this.document.baseURI);
        const failure = this.avatarLoadFailure();
        return {
            account,
            avatarUrl: failure?.userId === account.id && failure.url === url ? null : url,
            initials: this.buildInitials(account),
            sections: buildAdminUserSections(
                account,
                String(this.passwordChangeLabel()),
                String(this.lastActiveLabel()),
                String(this.unknownActivityLabel()),
            ),
        };
    });
    protected readonly canImpersonate = computed(() => {
        const user = this.user();
        return user !== null && (user.deletedAt === null || user.deletedAt === undefined) && !user.roles.includes('Admin');
    });
    protected readonly canNotify = computed(() => {
        const user = this.user();
        return user !== null && user.isActive && (user.deletedAt === null || user.deletedAt === undefined);
    });

    protected reload(): void {
        this.loadVersion.update(version => version + 1);
    }

    protected onAvatarError(userId: AdminUser['id'], url: string): void {
        const details = this.details();
        if (details?.account.id === userId && details.avatarUrl === url) {
            this.avatarLoadFailure.set({ userId, url });
        }
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

    protected sendNotification(user: AdminUser): void {
        if (this.canNotify()) {
            this.dialogs.open(AdminUserNotificationDialogComponent, { size: 'sm', data: user });
        }
    }

    private buildInitials(user: AdminUser): string {
        const initials = `${user.firstName?.trim()[0] ?? ''}${user.lastName?.trim()[0] ?? ''}`;
        return (initials.length > 0 ? initials : (user.email?.trim().at(0) ?? '?')).toUpperCase();
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
                url.hash = new URLSearchParams({ impersonationCode: result.code }).toString();
                this.document.defaultView?.open(url.toString(), '_blank', 'noopener,noreferrer');
            });
    }
}
