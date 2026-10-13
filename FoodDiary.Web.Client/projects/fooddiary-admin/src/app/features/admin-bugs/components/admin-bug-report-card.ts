import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent, FdUiStatusBadgeComponent } from 'fd-ui-kit';

import {
    conciseReportSummary,
    hasReadableReportSubject,
    optionalReportText,
    reportStatusPresentation,
} from '../lib/admin-bug-presentation';
import type { AdminBugReport } from '../models/admin-bug-report';
import { AdminBugReportDetailsComponent } from './admin-bug-report-details';
@Component({
    selector: 'fd-admin-bug-report-card',
    imports: [DatePipe, RouterLink, TranslatePipe, FdUiCardComponent, FdUiStatusBadgeComponent, AdminBugReportDetailsComponent],
    styleUrl: './admin-bug-report-card.scss',
    templateUrl: './admin-bug-report-card.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminBugReportCardComponent {
    public readonly report = input.required<AdminBugReport>();
    public readonly pullRequestUrl = input<string | null>(null);
    protected readonly title = computed(() => {
        const title = optionalReportText(this.report().title);
        return title !== null && hasReadableReportSubject(title)
            ? title
            : hasReadableReportSubject(this.report().subject)
              ? this.report().subject
              : null;
    });
    protected readonly preview = computed(() =>
        this.report().contentExpired ? '' : (optionalReportText(this.report().shortSummary) ?? conciseReportSummary(this.report().summary)),
    );
    protected readonly status = computed(() => reportStatusPresentation(this.report().status));
    protected readonly pullRequestNumber = computed(
        () => this.pullRequestUrl()?.match(/\/(?:pull|merge_requests)\/(\d+)\/?$/u)?.[1] ?? null,
    );
}
