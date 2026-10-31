import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { disabled, form, FormField, FormRoot, required } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import {
    FD_UI_DIALOG_DATA,
    FdUiButtonComponent,
    FdUiDialogComponent,
    FdUiDialogFooterDirective,
    FdUiDialogRef,
    FdUiSelectComponent,
} from 'fd-ui-kit';
import { firstValueFrom } from 'rxjs';

import { AdminUsersFacade } from '../lib/admin-users.facade';
import type { AdminTestNotification, AdminTestNotificationType } from '../models/admin-test-notification';
import type { AdminUser } from '../models/admin-user.models';

const NOTIFICATION_TYPES: AdminTestNotificationType[] = [
    'FastingCompleted',
    'FastingCheckInReminder',
    'EatingWindowStarted',
    'FastingWindowStarted',
];
const DEFAULT_DELAY_SECONDS = 15;
const HALF_MINUTE_SECONDS = 30;
const MINUTE_SECONDS = 60;
const FIVE_MINUTES_SECONDS = 300;
const DELAYS = [1, DEFAULT_DELAY_SECONDS, HALF_MINUTE_SECONDS, MINUTE_SECONDS, FIVE_MINUTES_SECONDS];

@Component({
    selector: 'fd-admin-user-notification-dialog',
    imports: [FormField, FormRoot, TranslatePipe, FdUiButtonComponent, FdUiDialogComponent, FdUiDialogFooterDirective, FdUiSelectComponent],
    templateUrl: './admin-user-notification-dialog.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminUserNotificationDialogComponent {
    private readonly dialogRef = inject<FdUiDialogRef<AdminUserNotificationDialogComponent, boolean>>(FdUiDialogRef);
    private readonly user = inject<AdminUser>(FD_UI_DIALOG_DATA);
    private readonly users = inject(AdminUsersFacade);
    private readonly translate = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly labels = toSignal(this.translate.stream('ADMIN_NOTIFICATION.TITLE'));

    protected readonly recipient = this.user.email ?? this.user.username ?? this.user.id;
    protected readonly isSubmitting = signal(false);
    protected readonly failed = signal(false);
    protected readonly scheduled = signal(false);
    protected readonly types = computed(() => {
        this.labels();
        return NOTIFICATION_TYPES.map(value => ({ value, label: String(this.translate.instant(`ADMIN_NOTIFICATION.TYPES.${value}`)) }));
    });
    protected readonly delays = computed(() => {
        this.labels();
        return DELAYS.map(value => ({ value, label: String(this.translate.instant(`ADMIN_NOTIFICATION.DELAYS.${value}`)) }));
    });
    private readonly submitFormAsync = async (): Promise<void> => {
        await this.submitAsync();
    };
    protected readonly formModel = signal<AdminTestNotification>({ type: 'FastingCompleted', delaySeconds: DEFAULT_DELAY_SECONDS });
    protected readonly form = form(
        this.formModel,
        path => {
            disabled(path, { when: () => this.isSubmitting() || this.scheduled() });
            required(path.type);
            required(path.delaySeconds);
        },
        { submission: { action: this.submitFormAsync } },
    );

    protected close(): void {
        if (!this.isSubmitting()) {
            this.dialogRef.close(this.scheduled());
        }
    }

    private async submitAsync(): Promise<void> {
        if (this.isSubmitting() || this.scheduled() || this.form().invalid()) {
            return;
        }
        this.failed.set(false);
        this.isSubmitting.set(true);
        try {
            await firstValueFrom(
                this.users.scheduleTestNotification(this.user.id, this.formModel()).pipe(takeUntilDestroyed(this.destroyRef)),
            );
            this.scheduled.set(true);
        } catch {
            this.failed.set(true);
        } finally {
            this.isSubmitting.set(false);
        }
    }
}
