import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent, FdUiStatusBadgeComponent } from 'fd-ui-kit';

import type { AdminAuditRunSummary } from '../models/admin-bug-report';

const STATUS_KEYS: Record<string, string> = {
    running: 'ADMIN_BUGS.AUDIT_RUNNING',
    clean: 'ADMIN_BUGS.AUDIT_CLEAN',
    findings: 'ADMIN_BUGS.AUDIT_FINDINGS',
    incomplete: 'ADMIN_BUGS.AUDIT_INCOMPLETE',
    failed: 'ADMIN_BUGS.AUDIT_FAILED',
    expired: 'ADMIN_BUGS.AUDIT_EXPIRED',
};

@Component({
    selector: 'fd-admin-audit-status',
    imports: [DatePipe, TranslatePipe, FdUiCardComponent, FdUiStatusBadgeComponent],
    templateUrl: './admin-audit-status.html',
    styleUrl: './admin-audit-status.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminAuditStatusComponent {
    public readonly audit = input.required<AdminAuditRunSummary | null>();
    protected readonly statusKey = computed(() => {
        const audit = this.audit();
        if (audit === null) {
            return 'ADMIN_BUGS.AUDIT_NONE';
        }
        return audit.isStale ? 'ADMIN_BUGS.AUDIT_STALE' : (STATUS_KEYS[audit.status] ?? 'ADMIN_BUGS.AUDIT_UNKNOWN');
    });
    protected readonly accent = computed(() => {
        const audit = this.audit();
        return audit?.status === 'clean' && !audit.isStale && !audit.contentExpired ? 'success' : 'default';
    });
    protected readonly tone = computed(() => {
        const audit = this.audit();
        if (audit === null || audit.contentExpired) {
            return 'muted';
        }
        if (audit.isStale || audit.status === 'findings' || audit.status === 'incomplete') {
            return 'warning';
        }
        if (audit.status === 'failed') {
            return 'danger';
        }
        return audit.status === 'clean' ? 'success' : 'muted';
    });
}
