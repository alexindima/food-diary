import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiDialogService } from 'fd-ui-kit';

import { AdminMailMessageDialogComponent } from '../dialogs/admin-mail-message-dialog';
import type { AdminDmarcView } from '../lib/admin-dmarc-loader';
import { DMARC_MAILBOX } from '../lib/admin-mail-inbox-preferences';
import type { AdminMailInboxMessageSummary } from '../models/admin-mail-inbox.data';

@Component({
    selector: 'fd-admin-dmarc-reports',
    imports: [DatePipe, RouterLink, TranslatePipe, FdUiButtonComponent],
    templateUrl: './admin-dmarc-reports.html',
    styleUrl: './admin-dmarc-reports.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: { class: 'fd-grid fd-gap-sm' },
})
export class AdminDmarcReportsComponent {
    private readonly dialog = inject(FdUiDialogService);
    public readonly data = input.required<AdminDmarcView>();
    public readonly retrying = input(false);
    public readonly retryAttachments = output();
    protected readonly reportMailbox = DMARC_MAILBOX;
    protected readonly statusLabels = {
        verified: 'ADMIN_DMARC.VERIFIED',
        unverified: 'ADMIN_DMARC.UNVERIFIED',
        unavailable: 'ADMIN_DMARC.UNAVAILABLE',
        'load-error': 'ADMIN_DMARC.LOAD_ERROR',
    } as const;

    protected openReport({ id, subject }: AdminMailInboxMessageSummary): void {
        this.dialog.open(AdminMailMessageDialogComponent, {
            preset: 'detail',
            panelClass: 'fd-admin-mail-message-dialog',
            size: 'xl',
            data: { id, subject },
        });
    }
}
