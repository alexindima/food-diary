import { DatePipe, formatDate } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { disabled, form, FormField, FormRoot, maxLength, required } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiCardComponent, FdUiInputComponent, FdUiTextareaComponent } from 'fd-ui-kit';
import { catchError, combineLatest, EMPTY, firstValueFrom, type Observable, startWith, Subject, switchMap } from 'rxjs';

import { AdminLoadErrorComponent } from '../../../shared/feedback/admin-load-error';
import { type AdminId, adminUtcInstant } from '../../../shared/models/semantics/admin-meaning';
import { AdminUsersFacade } from '../lib/admin-users.facade';
import type { AdminUserPremiumGrant } from '../models/admin-user.models';

const LOCAL_DATE_TIME_FORMAT = "yyyy-MM-dd'T'HH:mm";
const REASON_MAX_LENGTH = 500;

@Component({
    selector: 'fd-admin-user-premium-grant',
    imports: [
        AdminLoadErrorComponent,
        DatePipe,
        FdUiButtonComponent,
        FdUiCardComponent,
        FdUiInputComponent,
        FdUiTextareaComponent,
        FormField,
        FormRoot,
        TranslatePipe,
    ],
    templateUrl: './admin-user-premium-grant.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminUserPremiumGrantComponent {
    public readonly userId = input.required<AdminId<'user'>>();

    private readonly users = inject(AdminUsersFacade);
    private readonly destroyRef = inject(DestroyRef);
    private readonly refresh = new Subject<void>();
    private readonly formModel = signal({ endsAtLocal: '', reason: '' });
    protected readonly grant = signal<AdminUserPremiumGrant | null | undefined>(undefined);
    protected readonly loadFailed = signal(false);
    protected readonly errorKey = signal<string | null>(null);
    protected readonly editing = signal(false);
    protected readonly confirmingRevoke = signal(false);
    protected readonly saving = signal(false);
    private readonly submitFormAsync = async (): Promise<void> => {
        await this.saveAsync();
    };
    protected readonly form = form(
        this.formModel,
        path => {
            disabled(path, { when: () => this.saving() });
            required(path.endsAtLocal);
            required(path.reason);
            maxLength(path.reason, REASON_MAX_LENGTH);
        },
        { submission: { action: this.submitFormAsync } },
    );

    public constructor() {
        combineLatest([toObservable(this.userId), this.refresh.pipe(startWith(undefined))])
            .pipe(
                switchMap(([userId]) => {
                    this.closeEditor();
                    this.grant.set(undefined);
                    this.loadFailed.set(false);
                    return this.users.getPremiumGrant(userId).pipe(
                        catchError(() => {
                            this.loadFailed.set(true);
                            return EMPTY;
                        }),
                    );
                }),
                takeUntilDestroyed(),
            )
            .subscribe(grant => {
                this.grant.set(grant);
            });
    }

    protected reload(): void {
        this.refresh.next();
    }

    protected edit(): void {
        const grant = this.grant();
        this.formModel.set({
            endsAtLocal: grant === null || grant === undefined ? '' : formatDate(grant.endsAtUtc, LOCAL_DATE_TIME_FORMAT, 'en-US'),
            reason: grant?.reason ?? '',
        });
        this.form().reset();
        this.errorKey.set(null);
        this.editing.set(true);
    }

    protected closeEditor(): void {
        this.editing.set(false);
        this.confirmingRevoke.set(false);
        this.errorKey.set(null);
    }

    protected confirmRevoke(): void {
        this.errorKey.set(null);
        this.confirmingRevoke.set(true);
    }

    protected async revokeAsync(): Promise<void> {
        await this.submitAsync(this.users.revokePremiumGrant(this.userId()));
    }

    private async saveAsync(): Promise<void> {
        const { endsAtLocal, reason } = this.formModel();
        const current = this.grant();
        const original =
            current !== null && current !== undefined && formatDate(current.endsAtUtc, LOCAL_DATE_TIME_FORMAT, 'en-US') === endsAtLocal;
        const date = new Date(original ? current.endsAtUtc : endsAtLocal);
        if (
            !Number.isFinite(date.getTime()) ||
            date.getTime() <= Date.now() ||
            formatDate(date, LOCAL_DATE_TIME_FORMAT, 'en-US') !== endsAtLocal
        ) {
            this.errorKey.set('ADMIN_USER_PREMIUM.DATE_INVALID');
            return;
        }
        const note = reason.trim();
        if (note.length === 0) {
            this.errorKey.set('ADMIN_USER_PREMIUM.REASON_INVALID');
            return;
        }
        await this.submitAsync(this.users.setPremiumGrant(this.userId(), { endsAtUtc: adminUtcInstant(date.toISOString()), reason: note }));
    }

    private async submitAsync(request: Observable<AdminUserPremiumGrant | null>): Promise<void> {
        if (this.saving()) {
            return;
        }
        const userId = this.userId();
        this.saving.set(true);
        this.errorKey.set(null);
        try {
            const grant = await firstValueFrom(request.pipe(takeUntilDestroyed(this.destroyRef)));
            if (!this.destroyRef.destroyed && this.userId() === userId) {
                this.grant.set(grant);
                this.closeEditor();
            }
        } catch {
            if (!this.destroyRef.destroyed && this.userId() === userId) {
                this.errorKey.set('ADMIN_USER_PREMIUM.SAVE_ERROR');
            }
        } finally {
            this.saving.set(false);
        }
    }
}
