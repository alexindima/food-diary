import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import type { AdminBugReport } from '../models/admin-bug-report';

@Component({
    selector: 'fd-admin-bug-report-details',
    imports: [TranslatePipe],
    templateUrl: './admin-bug-report-details.html',
    styleUrl: './admin-bug-report-details.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminBugReportDetailsComponent {
    public readonly report = input.required<AdminBugReport>();
}
