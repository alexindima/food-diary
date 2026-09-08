import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';

import type { AdminContentReport } from '../models/admin-moderation.data';

const MILLISECONDS_PER_DAY = 86400000;

@Component({
    selector: 'fd-admin-moderation-context',
    imports: [DatePipe, RouterLink, TranslatePipe],
    templateUrl: './admin-moderation-context.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminModerationContextComponent {
    public readonly report = input.required<AdminContentReport>();
    protected readonly ageDays = computed(() =>
        Math.max(0, Math.floor((Date.now() - Date.parse(this.report().createdAtUtc)) / MILLISECONDS_PER_DAY)),
    );
}
