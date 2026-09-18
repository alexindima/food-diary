import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { AdminLoadErrorComponent } from '../../../shared/feedback/admin-load-error';
import type { DetailSection } from '../lib/admin-user-sections';
import type { AdminUserLoginEvent, AdminUserRoleAuditEvent } from '../models/admin-user.models';

@Component({
    selector: 'fd-admin-user-details-body',
    imports: [DatePipe, TranslatePipe, AdminLoadErrorComponent],
    templateUrl: './admin-user-details-body.html',
    styleUrl: './admin-user-details-body.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminUserDetailsBodyComponent {
    public readonly activityLoading = input(false);
    public readonly activityFailed = input(false);
    public readonly retry = output();
    public readonly roleAuditEvents = input.required<AdminUserRoleAuditEvent[]>();
    public readonly loginEvents = input.required<AdminUserLoginEvent[]>();
    public readonly sections = input.required<DetailSection[]>();

    protected describeRoleActor(event: AdminUserRoleAuditEvent): string {
        if (event.actorEmail !== null && event.actorEmail !== undefined && event.actorEmail.trim().length > 0) {
            return event.actorEmail;
        }

        if (event.actorUserId !== null && event.actorUserId !== undefined && event.actorUserId.trim().length > 0) {
            return event.actorUserId;
        }

        return event.source;
    }
}
