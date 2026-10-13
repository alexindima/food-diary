import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { safeReportPullRequestUrl } from '../lib/admin-bug-presentation';
import type { AdminBugReportPage } from '../models/admin-bug-report';
import { AdminAuditStatusComponent } from './admin-audit-status';
import { AdminBugReportCardComponent } from './admin-bug-report-card';

@Component({
    selector: 'fd-admin-bug-report-list',
    imports: [DatePipe, TranslatePipe, AdminAuditStatusComponent, AdminBugReportCardComponent],
    templateUrl: './admin-bug-report-list.html',
    styleUrl: './admin-bug-report-list.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminBugReportListComponent {
    public readonly data = input.required<AdminBugReportPage>();
    public readonly source = input.required<string>();
    protected readonly safePullRequestUrl = safeReportPullRequestUrl;
    protected readonly groups = computed(() => {
        const reports = this.data().items;
        if (this.source() !== 'logs') {
            return [{ id: 'email', fromUtc: null, toUtc: null, items: reports }];
        }
        const groups = new Map<string, { id: string; fromUtc: string | null; toUtc: string | null; items: AdminBugReportPage['items'] }>();
        for (const report of reports) {
            const id = report.auditRunId ?? report.id;
            let group = groups.get(id);
            if (group === undefined) {
                group = { id, fromUtc: report.auditFromUtc, toUtc: report.auditToUtc, items: [] };
                groups.set(id, group);
            }
            group.items.push(report);
        }
        return [...groups.values()];
    });
}
