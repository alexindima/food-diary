import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';

import type { AdminBugReport } from '../models/admin-bug-report';
@Component({
    selector: 'fd-admin-bug-report-card',
    imports: [DatePipe, RouterLink, TranslatePipe],
    styleUrl: '../../../shared/admin-records.scss',
    templateUrl: './admin-bug-report-card.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminBugReportCardComponent {
    public readonly report = input.required<AdminBugReport>();
    public readonly pullRequestUrl = input<string | null>(null);
}
