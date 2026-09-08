import { DatePipe, DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiCardComponent, FdUiDialogService } from 'fd-ui-kit';
import { catchError, combineLatest, of, startWith, Subject, switchMap } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { AdminLoadErrorComponent } from '../../../shared/feedback/admin-load-error';
import { AdminUserDetailsDialogComponent } from '../dialogs/admin-user-details-dialog';
import { AdminUserEditDialogComponent } from '../dialogs/admin-user-edit-dialog';
import { AdminUserImpersonationDialogComponent } from '../dialogs/admin-user-impersonation-dialog';
import { AdminUserSetPasswordDialogComponent } from '../dialogs/admin-user-set-password-dialog';
import { AdminUsersFacade } from '../lib/admin-users.facade';
import type { AdminImpersonationStart, AdminUser } from '../models/admin-user.models';

@Component({
    selector: 'fd-admin-user-page',
    imports: [DatePipe, RouterLink, TranslatePipe, FdUiButtonComponent, FdUiCardComponent, AdminLoadErrorComponent],
    templateUrl: './admin-user-page.html',
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

    public constructor() {
        combineLatest([this.route.paramMap, this.refresh.pipe(startWith(undefined))])
            .pipe(
                switchMap(([params]) => {
                    this.user.set(null);
                    this.failed.set(false);
                    return this.users.getUser(params.get('id') ?? '').pipe(
                        catchError(() => {
                            this.failed.set(true);
                            return of(null);
                        }),
                    );
                }),
                takeUntilDestroyed(),
            )
            .subscribe(user => {
                this.user.set(user);
            });
    }

    protected reload(): void {
        this.refresh.next();
    }

    protected edit(user: AdminUser): void {
        this.dialogs
            .open(AdminUserEditDialogComponent, { size: 'sm', data: user })
            .afterClosed()
            .subscribe(updated => {
                if (updated === true) {
                    this.reload();
                }
            });
    }

    protected details(user: AdminUser): void {
        this.dialogs
            .open(AdminUserDetailsDialogComponent, { size: 'xl', data: user })
            .afterClosed()
            .subscribe(action => {
                if (action === 'edit') {
                    this.edit(user);
                }
                if (action === 'setPassword') {
                    this.dialogs.open(AdminUserSetPasswordDialogComponent, { size: 'sm', data: user });
                }
                if (action === 'impersonate') {
                    this.impersonate(user);
                }
            });
    }

    private impersonate(user: AdminUser): void {
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
